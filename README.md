@[TOC](3D Game Programming & Design：与游戏世界交互)

# 编程实践
## 作业要求
1、编写一个简单的鼠标打飞碟（Hit UFO）游戏

游戏内容要求：
游戏有 n 个 round，每个 round 都包括10 次 trial；
每个 trial 的飞碟的色彩、大小、发射位置、速度、角度、同时出现的个数都可能不同。它们由该 round 的 ruler 控制；
每个 trial 的飞碟有随机性，总体难度随 round 上升；
鼠标点中得分，得分规则按色彩、大小、速度不同计算，规则可自由设定。
游戏的要求：
使用带缓存的工厂模式管理不同飞碟的生产与回收，该工厂必须是场景单实例的！具体实现见参考资源 Singleton 模板类
近可能使用前面 MVC 结构实现人机交互与游戏模型分离
如果你的使用工厂有疑问，参考：弹药和敌人：减少，重用和再利用

## 代码分析
因为本次作业难度还是比较大的，所以在代码结构上[参考](https://blog.csdn.net/tangyt77/article/details/79977806)了前辈的博客，设计的UML图如下：![在这里插入图片描述](https://img-blog.csdnimg.cn/20191008232546434.png?x-oss-process=image/watermark,type_ZmFuZ3poZW5naGVpdGk,shadow_10,text_aHR0cHM6Ly9ibG9nLmNzZG4ubmV0L3dlaXhpbl80MDM3NzY5MQ==,size_16,color_FFFFFF,t_70)
本次作业主要涉及以下几个类：
- DiskData（）；用于记录飞碟的相关数据 ，定义飞碟运动改变的时候的属性。
```javascript
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiskData : MonoBehaviour {
	public float size;//飞碟大小
	public Color color;//飞碟颜色
	public float speed;//飞碟速度
}
```
- DiskFactory（）；
	飞碟的生产和回收的工厂类。工厂里面应该实现两个功能：getDisk(ruler) 和FreeDisk(disk)。采用两个链表分别表示生产的飞碟库和回收的飞碟库，在生产飞碟时先判断回收库中是否有可利用的飞碟，若有，则重新利用飞碟，若没有则生产新的飞碟。回收飞碟时，在生产库中删除飞碟，然后将该飞碟的数据添加到回收库中循环利用。
```javascript
public class DiskFactory : MonoBehaviour {

	private List<GameObject> used = new List<GameObject>();//存储正在使用的飞碟
	private List<GameObject> free = new List<GameObject>();//存储使用完了被回收的飞碟
	private Color[] color = { Color.red, Color.green, Color.blue, Color.yellow };//颜色数组用于随机分配颜色

    //生产飞碟，先从回收部分取，若回收的部分为空，才从资源加载新的飞碟
    public GameObject GetDisk(int ruler)
	{
		GameObject a_disk;
		if (free.Count > 0)
		{
			a_disk = free[0];
			free.Remove(free[0]);
		}
		else
		{
			a_disk = GameObject.Instantiate(Resources.Load("prefabs/Disk")) as GameObject;
			Debug.Log(a_disk);
		}

		a_disk.GetComponent<DiskData>().size = UnityEngine.Random.Range(0, 7-ruler);
		a_disk.GetComponent<DiskData>().color = color[UnityEngine.Random.Range(0, 4)];
		a_disk.GetComponent<DiskData>().speed = UnityEngine.Random.Range(10+ruler, 18+ruler);

		a_disk.transform.localScale = new Vector3(a_disk.GetComponent<DiskData>().size * 2, a_disk.GetComponent<DiskData>().size * 0.1f, a_disk.GetComponent<DiskData>().size * 2);
		a_disk.GetComponent<Renderer>().material.color = a_disk.GetComponent<DiskData>().color;
		a_disk.SetActive(true);

		used.Add(a_disk);
		return a_disk;
	}

	//回收飞碟
	public void FreeDisk(GameObject disk)
	{
		for(int i = 0; i < used.Count; i++)
		{
			if(used[i] == disk)
			{
				disk.SetActive(false);
				used.Remove(used[i]);
				free.Add(disk);
			}
		}
	}
}
```
- Singleten< T > 
工厂模式用于管理不同飞碟的生产与回收，该工厂是场景单实例。运用模板，可以为每个 MonoBehaviour子类 创建一个对象的实例：Singleten<T> 场景单实例为飞碟工厂，分数以及动作提供实例。场景单实例的使用很简单，你仅需要将 MonoBehaviour 子类对象挂载任何一个游戏对象上即可。然后在任意位置使用代码 Singleton<YourMonoType>.Instance 获得该对象。
```javascript
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> where T : MonoBehaviour
{
	private static T instance;

	public static T Instance
	{
		get
		{
			if (instance == null)
			{
				instance = (T)Object.FindObjectOfType(typeof(T));
				if (instance == null)
				{
					Debug.LogError("Can't find instance of " + typeof(T));
				}
			}
			return instance;
		}
	}
}
```
- RoundActionManager（）；飞碟飞行的动作管理类，当场景控制器需要飞碟飞行的时候，调用动作管理类的方法，让飞碟飞行 。
```javascript
public class RoundActionManager : SSActionManager, ISSActionCallback
{
	public RoundController scene;
	public MoveToAction action1, action2;
	public SequenceAction saction;
	float speed;

	//该函数设置了起点和终点的坐标
	public void addRandomAction(GameObject gameObj)
	{
		int[] X = { -20, 20 };
		int[] Y = { -5, 5 };
		int[] Z = { -20, -20 };

		// 随机生成起始点和终点
		Vector3 starttPos = new Vector3(
			UnityEngine.Random.Range(-20, 20),
			UnityEngine.Random.Range(-5, 5),
			UnityEngine.Random.Range(50, 10)
		);

		gameObj.transform.position = starttPos;

		Vector3 randomTarget = new Vector3(
			X[UnityEngine.Random.Range(0, 2)],
			Y[UnityEngine.Random.Range(0, 2)],
			Z[UnityEngine.Random.Range(0, 2)]
		);

		MoveToAction action = MoveToAction.getAction(randomTarget, gameObj.GetComponent<DiskData>().speed);

		RunAction(gameObj, action, this);
	}

	protected  void Start()
	{
		scene = (RoundController)SSDirector.getInstance().currentScenceController;
		scene.actionManager = this;
	}

	protected new void Update()
	{
		base.Update();
	}

	public void actionDone(SSAction source)
	{
		Debug.Log("Done");
	}
}
```
- RoundController（）；场景控制类。这个类中的函数比较多，分别对应start，restart，暂停，shoot等等功能的具体操作。
	- 在这个类当中我们首先加入了一个倒计时3秒的功能，使得游戏体验更好，给玩家一个缓冲的时间。
		```javascript
			public int CoolTimes = 3;
			public Text GameText;//倒计时文本
		```
	- 	除去这个三秒倒计时外，每个round有60秒的时间，游戏过程中倒计时也会显示在页面左侧。
		```javascript
			IEnumerator DoCountDown()
			{
				while (leaveSeconds >= 0)
				{
					if (leaveSeconds >= 60) {
						GameText.text = (leaveSeconds - 60).ToString ();
					} else {
						GameText.text = "";
					}
					yield return new WaitForSeconds(1);
					leaveSeconds--;
				}
			}
		```
	- 关于射击功能的定义，用户在游戏状态为开始或者继续时，才能左键射击，击中飞碟会有爆炸碎片效果。		
		```javascript
			public void shoot()
			{
				if (Input.GetMouseButtonDown(0) && (state == State.START || state == State.CONTINUE))
				{
					Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
					RaycastHit hit;
					if (Physics.Raycast(ray, out hit))
					{
						if ((SSDirector.getInstance().currentScenceController.state == State.START || SSDirector.getInstance().currentScenceController.state == State.CONTINUE))
						{
							shootAtSth = hit.transform.gameObject;
		
							explosion.transform.position = hit.collider.gameObject.transform.position;
							explosion.GetComponent<Renderer>().material = hit.collider.gameObject.GetComponent<Renderer>().material;
							explosion.GetComponent<ParticleSystem>().Play();
						}
					}
				}
			}
		```
	- RecycleDisk()检查需不需要回收飞碟
		```javascript
		public void RecycleDisk()
			{
				for(int i = 0; i < disks.Count; i++)
				{
					if( disks[i].transform.position.z < -18)
					{
						diskFactory.FreeDisk(disks[i]);
						disks.Remove(disks[i]);
					}
				}
			}
		```

	- 在Resume和Pause两个函数中，用到了协程的知识，类似于Python中的挂起，关键字是yield。指的是在一个线程内，一个程序中断去执行另一个程序，有点类似于CPU中断。这样减少了切换线程带来的负担，同时不需要多线程中的锁机制，因为不存在同时写的问题。当程序执行到StopAllCoroutines()函数时，程序执行中断并挂起，这时执行的yield return语句，停止计时，等到StartCoroutines()时再从当前中断的地方继续执行。

		 ```javascript
			public void Pause()
			{
				state = State.PAUSE;
				CoolTimes = 3;
				StopAllCoroutines();
				for (int i = 0; i < disks.Count; i++)
				{
					disks[i].SetActive(false);
				}
			}
			public void Resume()
			{
				StartCoroutine(DoCountDown());         
				state = State.CONTINUE;
				for (int i = 0; i < disks.Count; i++)
				{
					disks[i].SetActive(true);
				}
			}
		```
	- Judge()：判断游戏状态，是否射中以及够不够分数进入下一回合。
		```javascript
		public void Judge()
			{
				if(shootAtSth != null && shootAtSth.transform.tag == "Disk" && shootAtSth.activeInHierarchy)//射中飞碟
				{
					scoreRecorder.Record(shootAtSth);
					diskFactory.FreeDisk(shootAtSth);
					shootAtSth = null;
				}
		
				if(scoreRecorder.getScore() > 500 * round)//每关500分才能进入下一关，重新倒数60秒
				{
					round++;
					leaveSeconds = count = 60;
				}
		
				if (round == 4) 
				{
					StopAllCoroutines();
					state = State.WIN;
				}
				else if (leaveSeconds == 0 && scoreRecorder.getScore() < 500 * round) //时间到，分数不够，输了
				{
					StopAllCoroutines();
					state = State.LOSE;
				} 
				else
					state = State.CONTINUE;
		
			}
		```

- ScoreRecorder（）；分数记录，设定得分规则。
这里我们设定的游戏规则是根据速度、飞碟大小和颜色的不同给分：
1）速度越快，飞碟越小则得分越高
2）红色+50，绿色+40，蓝色+30，黄色+10
然后在重新开始游戏的时候有一个分数置零的操作。
```javascript
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreRecorder : MonoBehaviour {
	private float score;

	public float getScore()
	{
		return score;
	}

	public void Record(GameObject disk)
	{
		score += (100 - disk.GetComponent<DiskData>().size *(20 - disk.GetComponent<DiskData>().speed));

		//根据颜色加分
		Color c = disk.GetComponent<DiskData>().color;
		switch (c.ToString())
		{
		case "red":
			score += 50;
			break;
		case "green":
			score += 40;
			break;
		case "blue":
			score += 30;
			break;
		case "yellow":
			score += 10;
			break;
		}
	}

	public void Reset()
	{
		score = 0;
	}
}

```
- SSDirector（）；导演类，这个和上次的牧师与魔鬼类似
```javascript
public class SSDirector : System.Object
	{
		public static SSDirector _instance;
		public ISceneController currentScenceController { get; set; }
		public bool running { get; set; }


		public static SSDirector getInstance()
		{
			if (_instance == null)
			{
				_instance = new SSDirector();
			}
			return _instance;
		}

		public int getFPS()
		{
			return Application.targetFrameRate;
		}

		public void setFPS(int fps)
		{
			Application.targetFrameRate = fps;
		}

		public void NextScene()
		{
			Debug.Log("抱歉，没下一个场景了");
		}
	}
```
- UserGUI（）；用户交互类，主要就是一些游戏界面设置，就不贴代码了，见GitHub。

- 最后
[视频链接-lose🔗](https://pan.baidu.com/s/1OSXf_q0FfOudWEw0LVEpFw)
[视频链接-win🔗](https://pan.baidu.com/s/18hEiPcpG0MGBDyF8WQcvwQ)
[我的Github代码传送门](https://github.com/ZhengweiZhao/Unity3D-HW2)

