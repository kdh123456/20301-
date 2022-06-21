using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : Player
{
	[Header("Player Move Speed")]
	[SerializeField]
	private float moveSpeed;

	[Header("Player Run Speed")]
	[SerializeField]
	private float runSpeed;

	//CharacterController ĳ�� �غ�
	private CharacterController controllerCharacter = null;

	//ĳ���� CollisionFlags �ʱⰪ ����
	private CollisionFlags collisionFlagsCharacter = CollisionFlags.None;

	//ĳ���� �߷°�
	private float gravity = 9.8f;

	//ĳ���� �߷� �ӵ� ��
	private float verticalSpd = 0f;

	//ĳ���� ���� �̵� ���� �ʱⰪ ����
	private Vector3 MoveDirect = Vector3.zero;
	//ĳ���� CollisionFlages�ʱⰪ
	private CollisionFlags collisionFlags = CollisionFlags.None;
	//ĳ���� �̵� ���� ȸ�� �ӵ� ����
	public float DirectRotateSpd = 100.0f;
	//���� ĳ���� �̵� ���� �� 
	private Vector3 vecNowVelocity = Vector3.zero;
	//ĳ���� ���� ȸ�� �ӵ� ����
	public float BodyRotateSpd = 5.0f;
	//���� ������ ����
	[Range(0.01f, 5.0f)]
	public float VelocityChangeSpd = 0.1f;

	private float horizontal;
	private float vertical;
	private bool isRun;

	private EventParam eventParam;

	protected override void Start()
	{
		base.Start();
		EventManager.StartListening("MoveInput", GetMoveInput);
		controllerCharacter = GetComponent<CharacterController>();
	}
	void Update()
	{
		Move();
		BodyDirectChange();
		MoveAnimation();
		setGravity();
	}

	private void Move()
	{
		//���� ����
		Transform CameraTransform = Camera.main.transform;

		//���� ī�޶� �ٶ󺸰� �ִ� ������ ����󿡼� � �����ΰ�...
		Vector3 forward = CameraTransform.TransformDirection(Vector3.forward);
		forward.y = 0;

		//forward.z, forward.x
		Vector3 right = new Vector3(forward.z, 0.0f, -forward.x);      //������.z�� ����� .x�� ����������  <----�߿�!!!


		//ĳ���� �̵��ϰ��� �ϴ� '����'!
		Vector3 targetDirect = horizontal * right + vertical * forward;

		//�������, ��ǥ����, ȸ���ӵ�, ����Ǵ� �ӵ�
		MoveDirect = Vector3.RotateTowards(MoveDirect, targetDirect, DirectRotateSpd * Mathf.Deg2Rad * Time.deltaTime, 1000.0f);
		MoveDirect = MoveDirect.normalized;

		//�̵��ӵ�
		float spd = moveSpeed;

		if (isRun)
		{
			spd = runSpeed;
		}

		Vector3 vecGravity = new Vector3(0f, verticalSpd, 0f);

		//�������̵���
		Vector3 amount = (MoveDirect * spd * Time.deltaTime) + vecGravity;

		//�����̵�
		collisionFlags = playerController.Move(amount);
	}


	/// <summary>
	/// ���� �� �ɸ��� �̵� �ӵ� �������� ��  
	/// </summary>
	/// <returns>float</returns>
	float getNowVelocityVal()
	{
		//���� ĳ���Ͱ� ���� �ִٸ� 
		if (playerController.velocity == Vector3.zero)
		{
			//��ȯ �ӵ� ���� 0
			vecNowVelocity = Vector3.zero;
		}
		else
		{
			//��ȯ �ӵ� ���� ���� /
			Vector3 retVelocity = playerController.velocity;
			retVelocity.y = 0.0f;

			vecNowVelocity = Vector3.Lerp(vecNowVelocity, retVelocity, VelocityChangeSpd * Time.fixedDeltaTime);

		}
		//�Ÿ� ũ��
		return vecNowVelocity.magnitude;
	}

	void BodyDirectChange()
	{
		if (getNowVelocityVal() > 0.0f)
		{
			Vector3 newForward = playerController.velocity; //������� ���ʴ�� ������ �ʰ� ����Ǵ� ��, �񵿱���� �����Լ��� ������ ������ ���� ������� ���ÿ� ����
			newForward.y = 0.0f;

			transform.forward = Vector3.Lerp(transform.forward, newForward, BodyRotateSpd * Time.deltaTime);
		}
	}

	private void MoveAnimation()
	{
		if (horizontal != 0 || vertical != 0)
		{
			if (isRun)
			{
				playerState = PlayerState.Run;
				ani.SetBool("IsRun", true);
				return;
			}
			playerState = PlayerState.Walk;
			ani.SetBool("IsWalk", true);
			ani.SetBool("IsRun", false);
		}
		else if (horizontal == 0 && vertical == 0)
		{
			playerState = PlayerState.Idle;
			ani.SetBool("IsWalk", false);
			ani.SetBool("IsRun", false);
		}
	}

	private void GetMoveInput(EventParam eventParam)
	{
		isRun = eventParam.input.isRun;
		horizontal = eventParam.input.moveVector.x;
		vertical = eventParam.input.moveVector.y;
	}

	void setGravity()
	{
		if ((collisionFlagsCharacter & CollisionFlags.CollidedBelow) != 0)
		{
			verticalSpd = 0f;
		}
		else
		{
			verticalSpd -= gravity * Time.deltaTime;
		}
	}
}
