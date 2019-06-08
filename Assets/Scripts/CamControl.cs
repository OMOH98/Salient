using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamControl : MonoBehaviour {
    public GameObject player;
    public bool readInput = true;

    public float maxDist = 10f;
    public float minDist = 0.75f;
    public float rotationSpeed = 30f;
    public float changeDistSpeed = 2f;
    public float movementSpeed = 5f;
    public float timeToMoveToPlayer = 0.5f;

    public event System.Action onFollow;
    public event System.Action onBreakFollow;



    //[SerializeField]
    float alpha, beta, dist;
    Coroutine movementToPlayer = null;
    GameObject target;
    private bool _followPlayer;
    protected bool followPlayer
    {
        get { return _followPlayer; }
        set
        {
            if(player == null)
            {
                _followPlayer = false;
                return;
            }
            if(value)
            {
                if (!_followPlayer)
                {
                    if (movementToPlayer != null)
                    {
                        StopCoroutine(movementToPlayer);
                        movementToPlayer = null;
                    }
                    movementToPlayer = StartCoroutine(MoveTargetToPosition(player.transform, target.transform, timeToMoveToPlayer, () =>
                    {
                        target.transform.SetParent(player.transform);
                        target.transform.localPosition = Vector3.zero;
                    }));
                    onFollow?.Invoke();
                }

            }
            else if(_followPlayer)
            {
                if (movementToPlayer != null)
                {
                    StopCoroutine(movementToPlayer);
                    movementToPlayer = null;
                }
                target.transform.SetParent(null);
                onBreakFollow?.Invoke();
            }
            _followPlayer = value;
        }
    }

    public void EnableInput() { readInput = true; }
    public void DisableInput() { readInput = false; }


	protected virtual void Start () {
        
        alpha = 70f;
        dist = 30f;
        CreateTarget();
        transform.SetParent(null);
	}
    private void CreateTarget()
    {
        target = new GameObject("CamFreelookTarget");
        if (player == null)
        {
            var initialPosition = transform.TransformDirection(Vector3.forward * dist) + transform.position;
            target.transform.position = initialPosition;
        }
        else target.transform.position = player.transform.position;
    }
	
	// Update is called once per frame
	protected virtual void Update () {
        if (target == null)
            CreateTarget();
        var input = Vector3.zero;
        if (readInput)
        {
            input = ReadMovement();
            ReadAndApplyManipulation();
        }
        ApplyMovement(input.x, input.y, input.z);
	}
    private void LateUpdate()
    {
        if (target == null /*|| !readInput*/)
            return;
        var alpha = Mathf.Deg2Rad * this.alpha;
        var beta = Mathf.Deg2Rad * this.beta;
        var elevation = Vector3.up * dist * Mathf.Cos(alpha);
        var displacement = (Vector3.right * Mathf.Cos(beta) + Vector3.forward * Mathf.Sin(beta)).normalized * dist * Mathf.Sin(alpha);
        transform.position = target.transform.position + elevation + displacement;
        transform.LookAt(target.transform.position + Vector3.up);
    }
    private void ReadAndApplyManipulation()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            var w = Input.GetAxis("Mouse ScrollWheel");
            var delta = 5f * rotationSpeed * Time.deltaTime * w;
            if (delta != 0f && alpha + delta > 5f && alpha + delta < 160f)
            {
                if (delta < 0f)
                    alpha += delta;
                else
                {
                    if (!Physics.Raycast(new Ray(transform.position, Vector3.down), 0.3f))
                        alpha += delta;
                }
            }
        }
        else if (/*Input.GetKey(KeyCode.LeftControl)*/true)
        {
            var delta = Input.GetAxis("Mouse ScrollWheel") * changeDistSpeed * Time.deltaTime;
            if (delta != 0f && dist + delta < maxDist && dist + delta > minDist)
                dist += delta;
        }
        var rot = Input.GetAxis("Rotation");
        beta += rot * rotationSpeed * Time.deltaTime;
    }

    private bool IsWhole(float f)
    {
        //return ((int)f) == f;
        return Mathf.Abs(Mathf.Abs(f) - 1f) > 1e-6f;
    }
    private Vector3 ReadMovement()
    {
        var v = Input.GetAxis("Vertical");
        var h = Input.GetAxis("Horizontal");
        var d = Input.GetAxis("Diagonal");
        if(!IsWhole(v)||!IsWhole(h)||!IsWhole(d))
        {
            followPlayer = false;
        }
        //if(Mathf.Abs(v) > 0f || Mathf.Abs(h) > 0f || Mathf.Abs(d) > 0)
        //{
        //    followPlayer = false;
        //}
        else if(Input.GetButtonDown("Follow"))
        {
            followPlayer = !followPlayer;
        }
        return new Vector3(v, d, h);
    }

    private void ApplyMovement(float v, float d, float h)
    {
        if (movementToPlayer == null)
        {
            var vertical = transform.TransformDirection(Vector3.forward);
            vertical.y = 0f;
            vertical.Normalize();

            var horizontal = Vector3.Cross(Vector3.up, vertical).normalized;

            var elevator = Vector3.up;
            target.transform.localPosition = target.transform.localPosition + (vertical * v + horizontal * h + elevator * d) * movementSpeed * Time.deltaTime;
        }
    }

    private IEnumerator MoveTargetToPosition(Transform whereToMove, Transform target, float time, System.Action OnBreak = null)
    {
        Vector3 oPos = target.position;
        float begin = Time.time, ptg = 0f;
        do
        {
            ptg = (Time.time - begin) / time;
            target.position = Vector3.Lerp(oPos, whereToMove.position, ptg);
            yield return null;
        } while (ptg <= 1f);

        if (movementToPlayer != null)
            movementToPlayer = null;
        if (OnBreak != null)
            OnBreak.Invoke();

        yield break;
    }
}
//public class CamMover:MonoBehaviour
//
//    float _alpha=20f;
//    float _beta;
//    float _dist=3f;
//    Vector3 _base;
//    bool _change;
//    public float angleToY
//    {
//        get { return _alpha; }
//        set
//        {
//            if (value > 90f)
//                _alpha = 90f;
//            else if (value < 90f)
//                _alpha = 0f;
//            else
//            {
//                _alpha = value;
//                _change = true;
//            }
//        }
//    }
//    public float angleToX
//    {
//        get { return _beta; }
//        set
//        {
//            _beta = value % 360f;
//            _change = true;
//        }
//    }
//    public float distanceToBase
//    {
//        get { return _dist; }
//        set
//        {
//            _dist = value;
//            _change = true;
//        }
//    }
//    public Vector3 basePosition
//    {
//        get { return _base; }
//        set
//        {
//            _change = _change || value != _base;
//            _base = value;
//        }
//    }

//    private void LateUpdate()
//    {
//        if (_change)
//        {
//            var alpha = Mathf.Deg2Rad * _alpha;
//            var beta = Mathf.Deg2Rad * _beta;
//            var elevation = Vector3.up * _dist * Mathf.Cos(alpha);
//            var displacement = (Vector3.right * Mathf.Cos(beta) + Vector3.forward * Mathf.Sin(beta)).normalized * _dist * Mathf.Sin(alpha);
//            transform.position = _base + elevation + displacement;
//            transform.LookAt(_base + Vector3.up);
//            _change = false;
//        }
//    }
//}