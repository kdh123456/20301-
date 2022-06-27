using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public enum PrincessState { None, Idle, Move, Wait, GoTarget, Atk, Damage, Die }
public class Princess : MonoBehaviour,OnHIt
{

	[Header("Ÿ�� ��ġ")]
	public Transform playertransform;

	private Transform princessTransform;

	public Vector3 posTarget = Vector3.zero;

	public Vector3 posLookAt = Vector3.zero;

	#region Basic
	[Header("�⺻�Ӽ�")]
	public PrincessState princessState = PrincessState.None;

	public float spdMove = 1f;

	public float basicSpeed = 1f;
	public float runSpeed = 1f;

	private Animator princessAnimator = null;
	#endregion

	#region Fight

	[Header("�����Ӽ�")]
	public int atk = 0;
	public int Atk { get => atk; set => atk = value; }

	public float RunRange = 0f;
	public float IdleRange = 1.5f;

	public GameObject effectDamage = null;

	public GameObject effectDie = null;

	[SerializeField]
	private SkinnedMeshRenderer skinnedMeshRenderer = null;
	#endregion

	EventParam eventParam;

	// Start is called before the first frame update
	void Start()
	{
		//ó�� ���� ������
		princessState = PrincessState.Idle;
		princessTransform = this.transform;
		//�ִϸ���, Ʈ������ ������Ʈ ĳ�� : �������� ã�� ������ �ʰ�
		princessAnimator = GetComponent<Animator>();
	}

	private void Update()
	{
		CkState();

		princessTransform.LookAt(playertransform);
	}

	/// <summary>
	/// �ذ� ���¿� ���� ������ �����ϴ� �Լ� 
	/// </summary>
	void CkState()
	{
		switch (princessState)
		{
			case PrincessState.Idle:
				//�̵��� ���õ� RayCast��
				setIdle();
				princessAnimator.SetBool("isWalk", false);
				break;
			case PrincessState.GoTarget:
			case PrincessState.Move:
				setMove();
				princessAnimator.SetBool("isWalk", true);
				break;

			default:
				break;
		}
	}

	void setIdle()
	{
		if (playertransform == null)
		{
			posTarget = new Vector3(princessTransform.position.x + Random.Range(-10f, 10f),
									princessTransform.position.y + 1000f,
									princessTransform.position.z + Random.Range(-10f, 10f)
				);
			Ray ray = new Ray(posTarget, Vector3.down);
			RaycastHit infoRayCast = new RaycastHit();
			if (Physics.Raycast(ray, out infoRayCast, Mathf.Infinity) == true)
			{
				posTarget.y = infoRayCast.point.y;
			}
			princessState = PrincessState.Move;
		}
		else
		{
			if (IdleRange < Mathf.Abs(playertransform.position.magnitude - princessTransform.position.magnitude))
				princessState = PrincessState.GoTarget;
		}
	}

	/// <summary>
	/// �ذ� ���°� �̵� �� �� �� 
	/// </summary>
	void setMove()
	{
		//����� ������ �� ������ ���� 
		Vector3 distance = Vector3.zero;

		//�ذ� ����
		switch (princessState)
		{
			//�ذ��� ���ƴٴϴ� ���
			case PrincessState.Move:
				//���� ���� ��ġ ���� ���ΰ� �ƴϸ�
				if (posTarget != Vector3.zero)
				{
					//��ǥ ��ġ���� �ذ� �ִ� ��ġ ���� ���ϰ�
					distance = posTarget - princessTransform.position;

					//���࿡ �����̴� ���� �ذ��� ��ǥ�� �� ���� ���� ���� 
					if (distance.magnitude <= IdleRange)
					{
						//��� ���� �Լ��� ȣ��
						princessState = PrincessState.Idle;
						//���⼭ ����
						return;
					}
					else if(distance.magnitude > RunRange)
					{
						spdMove = runSpeed;
					}
					else
					{
						spdMove = basicSpeed;
					}
				}
				break;
			//ĳ���͸� ���ؼ� ���� ���ƴٴϴ�  ���
			case PrincessState.GoTarget:
				//��ǥ ĳ���Ͱ� ���� ��
				if (playertransform != null)
				{
					//��ǥ ��ġ���� �ذ� �ִ� ��ġ ���� ���ϰ�
					distance = playertransform.transform.position - princessTransform.position;
					//���࿡ �����̴� ���� �ذ��� ��ǥ�� �� ���� ���� ���� 
					if (distance.magnitude < IdleRange)
					{
						//���ݻ��·� �����մ�.
						princessState = PrincessState.Idle;
						return;
					}
					else if (distance.magnitude > RunRange)
					{
						spdMove = runSpeed;
					}
					else
					{
						spdMove = basicSpeed;
					}
				}
				break;
			default:
				break;

		}

		//�ذ� �̵��� ���⿡ ũ�⸦ ���ְ� ���⸸ ����(normalized)
		Vector3 direction = distance.normalized;

		//������ x,z ��� y�� ���� �İ� ���Ŷ� ����
		direction = new Vector3(direction.x, 0f, direction.z);

		//�̵��� ���� ���ϱ�
		Vector3 amount = direction * spdMove * Time.deltaTime;

		//ĳ���� ��Ʈ���� �ƴ� Ʈ���������� ���� ��ǥ �̿��Ͽ� �̵�
		princessTransform.Translate(amount, Space.World);
		//ĳ���� ���� ���ϱ�
		//princessTransform.LookAt(posLookAt);
	}

	public void OnHit(int atk)
	{
		//������ ������ �ؾ��ϰ�
		if(!princessAnimator.GetBool("isDamage"))
		{
			princessAnimator.SetBool("isDamage", true);
			GameManager.Instance.PrincessData.hp -= atk;
			EventManager.TriggerEvent("HPPrincess", eventParam);
			//���� hp�ٵ� ���ְ�
			if (GameManager.Instance.PrincessData.hp <= 0)
			{
				this.gameObject.SetActive(false);
			}
		}
	}

	private void OnGUI()
	{
		GUIStyle gUIStyle = new GUIStyle();
		gUIStyle.fontSize = 40;
		gUIStyle.normal.textColor = Color.red;

		GUI.Label(new Rect(0, 100, 5, 5), "���� HP = " + GameManager.Instance.PrincessData.hp.ToString(), gUIStyle);
	}

	public void OnDmgAnmationFinished()
	{
		princessAnimator.SetBool("isDamage", false);
	}
}
