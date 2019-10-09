using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.game;

public interface ISSActionCallback
{
	void actionDone(SSAction source);
}

public class SSAction : ScriptableObject
{

	public bool enable = true;
	public bool destroy = false;

	public GameObject gameObject { get; set; }
	public Transform transform { get; set; }
	public ISSActionCallback callback { get; set; }

	public virtual void Start()
	{
		throw new System.NotImplementedException();
	}

	public virtual void Update()
	{
		throw new System.NotImplementedException();
	}
}

public class MoveToAction : SSAction
{
	public Vector3 target;
	public float speed;

	private MoveToAction() { }
	public static MoveToAction getAction(Vector3 target, float speed)
	{
		MoveToAction action = ScriptableObject.CreateInstance<MoveToAction>();
		action.target = target;
		action.speed = speed;
		return action;
	}

	public override void Update()
	{
		this.transform.position = Vector3.MoveTowards(this.transform.position, target, speed * Time.deltaTime);
		if (this.transform.position == target)
		{
			this.destroy = true;
			this.callback.actionDone(this);
		}
	}

	public override void Start() {}

}

public class SequenceAction : SSAction, ISSActionCallback
{
	public List<SSAction> sequence;
	public int repeat = -1; //-1表示无限循环，0表示只执行一遍，repeat> 0 表示重复repeat遍
	public int currentAction = 0;//当前动作列表里，执行到的动作序号

	public static SequenceAction getAction(int repeat, int currentActionIndex, List<SSAction> sequence)
	{
		SequenceAction action = ScriptableObject.CreateInstance<SequenceAction>();
		action.sequence = sequence;
		action.repeat = repeat;
		action.currentAction = currentActionIndex;
		return action;
	}

	public override void Update()
	{
		if (sequence.Count == 0) return;
		if (currentAction < sequence.Count)
		{
			sequence[currentAction].Update();
		}
	}

	public void actionDone(SSAction source)
	{
		source.destroy = false;
		this.currentAction++;
		if (this.currentAction >= sequence.Count)
		{
			this.currentAction = 0;
			if (repeat > 0) repeat--;
			if (repeat == 0)
			{
				this.destroy = true;
				this.callback.actionDone(this);
			}
		}
	}

	public override void Start()
	{
		foreach (SSAction action in sequence)
		{
			action.gameObject = this.gameObject;
			action.transform = this.transform;
			action.callback = this;
			action.Start();
		}
	}

	void OnDestroy()
	{
		foreach (SSAction action in sequence)
		{
			DestroyObject(action);
		}
	}
}


public class SSActionManager : MonoBehaviour
{
	private Dictionary<int, SSAction> actions = new Dictionary<int, SSAction>();
	private List<SSAction> waitingToAdd = new List<SSAction>();
	private List<int> watingToDelete = new List<int>();

	protected void Update()
	{
		foreach (SSAction ac in waitingToAdd)
		{
			actions[ac.GetInstanceID()] = ac;
		}
		waitingToAdd.Clear();

		foreach (KeyValuePair<int, SSAction> kv in actions)
		{
			SSAction ac = kv.Value;
			if (ac.destroy)
			{
				watingToDelete.Add(ac.GetInstanceID());
			}
			else if (ac.enable)
			{
				ac.Update();
			}
		}

		foreach (int key in watingToDelete)
		{
			SSAction ac = actions[key];
			actions.Remove(key);
			DestroyObject(ac);
		}
		watingToDelete.Clear();
	}

	public void RunAction(GameObject gameObject, SSAction action, ISSActionCallback whoToNotify)
	{
		action.gameObject = gameObject;
		action.transform = gameObject.transform;
		action.callback = whoToNotify;
		waitingToAdd.Add(action);
		action.Start();
	}

}

public class RoundActionManager : SSActionManager, ISSActionCallback
{
	public RoundController scene;
	public MoveToAction action1, action2;
	public SequenceAction saction;
	float speed;


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