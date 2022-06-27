using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class EnemyCtrl : MonoBehaviour,OnHIt
{
	public enum SkullState { None, Idle, Move, Wait, GoTarget, Atk, Damage, Die }
	private int count = 0;
	EventParam eventParam;

	[SerializeField]
	private GameObject objet;
	private Canvas hpbarcanvas;
	private GameObject hpbar;
	private Slider hpBar;

	#region Basic Variable
	[Header("�⺻ �Ӽ�")]
	public SkullState skullState = SkullState.None;

	public float spdMove = 1f;
	public GameObject targetCharactor = null;
	public Transform targetTransform = null;
	private Transform skullTransform = null;
	public Vector3 posTarget = Vector3.zero;

	private Animator ani = null;
	#endregion

	#region Fight Variable
	[Header("�����Ӽ�")]
	//�ذ� ü��
	public float maxHp;
	public float hp = 100;
	public int atk = 50;
	public int Atk { get => atk;  set => atk = value; }
	//�ذ� ���� �Ÿ�
	public float AtkRange = 1.5f;
	public ParticleSystem[] attackparticle;//�ذ� �ǰ� ����Ʈ
	public float radius;
	public LayerMask layerMask;
	public GameObject effectDamage = null;
	//�ذ� ���� ����Ʈ
	public GameObject effectDie = null;
	private SkinnedMeshRenderer skinnedMeshRenderer = null;
	#endregion

	private void Awake()
	{
		this.gameObject.transform.position = new Vector3(120, 0f, 142.6f);
	}
	void Start()
	{
		Init();
	}
	void Update()
	{
		CkState();
		AnimationCtrl();
		hpbar.transform.position = Camera.main.WorldToScreenPoint(new Vector3(transform.position.x, transform.position.y + 2, transform.position.z));
	}
	public void Init()
	{
		//ó�� ���� ������
		skullState = SkullState.Idle;

		//�ִϸ���, Ʈ������ ������Ʈ ĳ�� : �������� ã�� ������ �ʰ�
		ani = GetComponent<Animator>();
		skullTransform = GetComponent<Transform>();

		//��Ų�Ž� ĳ��
		skinnedMeshRenderer = skullTransform.Find("SoldierRen").GetComponent<SkinnedMeshRenderer>();
		EventManager.StartListening("Attaking", IsAttacked);

		for (int i = 0; i < attackparticle.Length; i++)
		{
			attackparticle[i].Pause();
		}

		effectDamage.GetComponent<ParticleSystem>().Pause();
		effectDie.GetComponent<ParticleSystem>().Pause();

		hpbarcanvas = GameObject.Find("EnemyCanvas").GetComponent<Canvas>();
		maxHp = hp;
		hpbar = Instantiate(objet, hpbarcanvas.transform);
		hpBar = hpbar.GetComponent<Slider>();
		UpdateSlider();
		hpBar.gameObject.SetActive(false);
	}

	#region Animation
	void AnimationCtrl()
	{
		//�ذ��� ���¿� ���� �ִϸ��̼� ����
		switch (skullState)
		{
			//���� �غ��� �� �ִϸ��̼� ��.
			case SkullState.Wait:
			case SkullState.Idle:
				//�غ� �ִϸ��̼� ����
				ani.SetBool("isAttack", false);
				ani.SetBool("isWalk", false);
				ani.SetBool("isDie", false);
				ani.SetBool("isDamage", false);
				break;
			//������ ��ǥ �̵��� �� �ִϸ��̼� ��.
			case SkullState.Move:
			case SkullState.GoTarget:
				//�̵� �ִϸ��̼� ����
				ani.SetBool("isWalk", true);
				ani.SetBool("isDie", false);
				ani.SetBool("isDamage", false);
				ani.SetBool("isAttack", false);
				break;
			//������ ��
			case SkullState.Atk:
				//���� �ִϸ��̼� ����
				ani.SetBool("isWalk", true);
				ani.SetBool("isDie", false);
				ani.SetBool("isDamage", false);
				ani.SetBool("isAttack", true);
				break;
			//�׾��� ��
			case SkullState.Die:
				//���� ���� �ִϸ��̼� ����
				ani.SetBool("isDie", true);
				ani.SetBool("isDamage", false);
				ani.SetBool("isAttack", false);
				ani.SetBool("isWalk", false);
				break;
			default:
				break;

		}
	}
	void OnAtkAnmationFinished()
	{
		ani.SetBool("isWalk", false);
		ani.SetBool("isDie", false);
		ani.SetBool("isDamage", false);
		ani.SetBool("isAttack", false);
		theAtk();
		skullState = SkullState.Idle;
	}

	void OnDmgAnmationFinished()
	{
		skullState = SkullState.Idle;
		this.transform.GetChild(this.transform.childCount-1).gameObject.GetComponent<ParticleSystem>().Pause();
		this.transform.GetChild(this.transform.childCount-1).gameObject.GetComponent<ParticleSystem>().Clear();
		ObjectPool.Instance.ReturnObject(PoolObjectType.DAMAGEDEFFECT, this.transform.GetChild(this.transform.childCount-1).gameObject);
		effectDamage.GetComponent<ParticleSystem>().Pause();		effectDie.GetComponent<ParticleSystem>().Pause();
		effectDie.GetComponent<ParticleSystem>().Clear();
	}

	void OnDieAnmationFinished()
	{
		gameObject.SetActive(false);
		ObjectPool.Instance.ReturnObject(PoolObjectType.Soldier, gameObject);
	}
	#endregion

	#region State
	void CkState()
	{
		switch (skullState)
		{
			case SkullState.Idle:
				//�̵��� ���õ� RayCast��
				setIdle();
				break;
			case SkullState.GoTarget:
			case SkullState.Move:
				setMove();
				break;
			case SkullState.Atk:
				setAtk();
				break;
			default:
				break;
		}
	}
	void setIdle()
	{
		if (targetCharactor == null)
		{
			posTarget = new Vector3(skullTransform.position.x + Random.Range(-10f, 10f),
									skullTransform.position.y + 1000f,
									skullTransform.position.z + Random.Range(-10f, 10f)
				);
			Ray ray = new Ray(posTarget, Vector3.down);
			RaycastHit infoRayCast = new RaycastHit();
			if (Physics.Raycast(ray, out infoRayCast, Mathf.Infinity) == true)
			{
				posTarget.y = infoRayCast.point.y;
			}
			skullState = SkullState.Move;
		}
		else
		{
			skullState = SkullState.GoTarget;
		}
	}

	void setMove()
	{
		//����� ������ �� ������ ���� 
		Vector3 distance = Vector3.zero;
		//��� ������ �ٶ󺸰� ���� �ִ��� 
		Vector3 posLookAt = Vector3.zero;

		//�ذ� ����
		switch (skullState)
		{
			//�ذ��� ���ƴٴϴ� ���
			case SkullState.Move:
				//���� ���� ��ġ ���� ���ΰ� �ƴϸ�
				if (posTarget != Vector3.zero)
				{
					//��ǥ ��ġ���� �ذ� �ִ� ��ġ ���� ���ϰ�
					distance = posTarget - skullTransform.position;

					//���࿡ �����̴� ���� �ذ��� ��ǥ�� �� ���� ���� ���� 
					if (distance.magnitude < AtkRange)
					{
						//��� ���� �Լ��� ȣ��
						StartCoroutine(setWait());
						//���⼭ ����
						return;
					}

					//��� ������ �ٶ� �� ����. ���� ����
					posLookAt = new Vector3(posTarget.x,
											//Ÿ���� ���� ���� ��찡 ������ y�� üũ
											skullTransform.position.y,
											posTarget.z);
				}
				break;
			//ĳ���͸� ���ؼ� ���� ���ƴٴϴ�  ���
			case SkullState.GoTarget:
				//��ǥ ĳ���Ͱ� ���� ��
				if (targetCharactor != null)
				{
					//��ǥ ��ġ���� �ذ� �ִ� ��ġ ���� ���ϰ�
					distance = targetCharactor.transform.position - skullTransform.position;
					//���࿡ �����̴� ���� �ذ��� ��ǥ�� �� ���� ���� ���� 
					if (distance.magnitude < AtkRange)
					{
						//���ݻ��·� �����մ�.
						skullState = SkullState.Atk;
						//���⼭ ����
						return;
					}
					//��� ������ �ٶ� �� ����. ���� ����
					posLookAt = new Vector3(targetCharactor.transform.position.x,
											//Ÿ���� ���� ���� ��찡 ������ y�� üũ
											skullTransform.position.y,
											targetCharactor.transform.position.z);
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
		skullTransform.Translate(amount, Space.World);
		//ĳ���� ���� ���ϱ�
		skullTransform.LookAt(posLookAt);
	}

	IEnumerator setWait()
	{
		//�ذ� ���¸� ��� ���·� �ٲ�
		skullState = SkullState.Wait;
		//����ϴ� �ð��� �������� �ʰ� ����
		float timeWait = Random.Range(1f, 3f);
		//��� �ð��� �־� ��.
		yield return new WaitForSeconds(timeWait);
		//��� �� �ٽ� �غ� ���·� ����
		skullState = SkullState.Idle;
	}

	void setAtk()
	{
		//�ذ�� ĳ���Ͱ��� ��ġ �Ÿ� 
		float distance = Vector3.Distance(targetTransform.position, skullTransform.position); //���̴�

		//���� �Ÿ����� �� ���� �Ÿ��� �־� ���ٸ� 
		if (distance > AtkRange + 0.5f)
		{
			//Ÿ�ٰ��� �Ÿ��� �־����ٸ� Ÿ������ �̵� 
			skullState = SkullState.GoTarget;
		}
	}
	#endregion
	void OnCkTarget(GameObject target)
	{
		//��ǥ ĳ���Ϳ� �Ķ���ͷ� ����� ������Ʈ�� �ְ� 
		targetCharactor = target;
		//��ǥ ��ġ�� ��ǥ ĳ������ ��ġ ���� �ֽ��ϴ�. 
		targetTransform = targetCharactor.transform;

		//��ǥ���� ���� �ذ��� �̵��ϴ� ���·� ����
		skullState = SkullState.GoTarget;

	}

	void theAtk()
	{
		for (int i = 0; i < attackparticle.Length; i++)
		{
			attackparticle[i].gameObject.SetActive(true);
		}
		for (int i = 0; i < attackparticle.Length; i++)
		{
			attackparticle[i].Clear();
			attackparticle[i].Play();
		}
		Collider[] a = Physics.OverlapSphere(this.transform.position, radius, layerMask);

		if (a.Length > 0)
		{
			for(int i = 0; i<a.Length; i++)
			{
				a[i].GetComponentInParent<OnHIt>().OnHit(Atk);
			}
		}
	}
	#region Hit
	private void OnTriggerEnter(Collider other)
	{
		//���࿡ �ذ��� ĳ���� ���ݿ� �¾Ҵٸ�
		if (other.gameObject.CompareTag("PlayerAtk") == true && count < eventParam.eventint)
		{
			ani.SetBool("isDie", false);
			ani.SetBool("isDamage", true);
			ani.SetBool("isAttack", false);
			ani.SetBool("isWalk", false);
			OnHit(other.GetComponentInParent<OnHIt>().Atk);
		}
	}
	IEnumerator Wait(GameObject obj)
	{
		yield return new WaitForSeconds(1f);
		obj.transform.position = this.transform.position;
	}
	void effectDamageTween()
	{
		Color colorTo = Color.red;

		skinnedMeshRenderer.material.DOColor(colorTo, 0f).OnComplete(OnDamageTweenFinished);
	}
	void OnDamageTweenFinished()
	{
		//Ʈ���� ������ �Ͼ������ Ȯ���� ������ �����ش�
		skinnedMeshRenderer.material.DOColor(Color.white, 2f);
	}

	void IsAttacked(EventParam events)
	{
		eventParam = events;
		if (eventParam.eventint == 0)
		{
			count = 0;
		}
	}

	public void OnHit(int atk)
	{
		count++;
		//�ذ� ü���� 10 ���� 
		hp -= atk;
		hpBar.gameObject.SetActive(true);
		UpdateSlider();
		if (hp > 0)
		{
			//�ǰ� ����Ʈ 
			skullState = SkullState.Damage;
			GameObject obj = ObjectPool.Instance.GetObject(PoolObjectType.DAMAGEDEFFECT);
			obj.transform.parent = this.transform;
			obj.transform.position = new Vector3(this.transform.position.x, this.transform.position.y + 1, this.transform.position.z);


			effectDamageTween();
			//ü���� 0 �̻��̸� �ǰ� �ִϸ��̼��� ���� �ϰ� 
		}
		else
		{
			//0 ���� ������ �ذ��� ���� ���·� �ٲپ��  
			skullState = SkullState.Die;
			effectDie.GetComponent<ParticleSystem>().Play();
			for(int i = 0; i<attackparticle.Length; i++)
			{
				attackparticle[i].gameObject.SetActive(false);
			}
			hpBar.gameObject.SetActive(false);
			GameManager.Instance.PlayerData.Money += 5;
			GameObject obj = ObjectPool.Instance.GetObject(PoolObjectType.HP);
			StartCoroutine(Wait(obj));
		}
	}

	public void UpdateSlider()
	{
		hpBar.maxValue = maxHp;
		hpBar.value = hp;
	}

	#endregion

	private void OnDestroy()
	{
		EventManager.StopListening("Attaking", IsAttacked);
	}
}