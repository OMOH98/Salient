using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Jurassic;
using UnityEngine.UI;


[RequireComponent(typeof(Rigidbody))]
public class Tank : MonoBehaviour
{
    public InputField code;
    public bool execute = false;

    [Header("Actuators")]
    public float speed = 5f;
    public float tankWidth = 2f;
    //public float goCenterHeight = 1.5f;
    public float rotationTreshold = 0.05f;
    public float turretAngularSpeed = 20f;
    public float radarAngularSpeed = 60f;
    public float heatPerShot = 0.2f; //heat treshold = 1f
    public float overheatFine = 0.2f;

    public Actions actions;

    private ScriptEngine engine = new ScriptEngine();
    private float initTime = 0f;   

    [System.Serializable]
    public class Actions
    {
        [Range(-0.5f, 1f)]
        public float leftTrackSpeed;
        [Range(-0.5f, 1f)]
        public float rightTrackSpeed;
        [Range(-1f, 1f)]
        public float towerAngularSpeed;
        [Range(-1f, 1f)]
        public float radarAngularSpeed;
        public bool fireAtWill;

        public void Validate()
        {
            leftTrackSpeed = Mathf.Clamp(leftTrackSpeed, -0.5f, 1f);
            rightTrackSpeed = Mathf.Clamp(rightTrackSpeed, -0.5f, 1f);
            towerAngularSpeed = Mathf.Clamp(towerAngularSpeed, -1f, 1f);
            radarAngularSpeed = Mathf.Clamp(radarAngularSpeed, -1f, 1f);
        }
    }

    [System.Serializable]
    public class Sensors
    {
        public enum ObjectCategory
        {
            Wall, Ground, Enemy, Ally, Neutral
        }
        public float azimuth;
        //public 
    }

    public void ExecOnce()
    {
        engine.Execute(code.text);
    }

    Rigidbody rb;
    // Start is called before the first frame update 
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        engine.EnableExposedClrTypes = true;
        System.Action<string> logger = (s) => Debug.Log(s);
        engine.SetGlobalFunction("log", logger);
        engine.SetGlobalValue("actions", actions);

        initTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Space))
        //    Debug.Break();
    }

    private void FixedUpdate()
    {
        engine.SetGlobalValue("sensors", new Sensors() { azimuth = transform.rotation.eulerAngles.y });
        
        //TODO: Validation of actions values;
        actions.Validate();

        if (execute)
            engine.Execute(code.text);
        Movement_PhysicsMess();
    }

    private void Movement_PhysicsMess()
    {
        if (Mathf.Abs(actions.leftTrackSpeed - actions.rightTrackSpeed) < rotationTreshold)
        {
            rb.AddForce(transform.forward * 2 * speed * Mathf.Min(actions.leftTrackSpeed, actions.rightTrackSpeed));
        }
        else
        {
            var force = transform.forward * speed * actions.leftTrackSpeed;
            var point = transform.TransformPoint(Vector3.left * tankWidth * 0.5f/* + Vector3.down * goCenterHeight*/);
            rb.AddForceAtPosition(force, point, ForceMode.Force);
            //Debug.Log($"Force: {force.magnitude}; dist: {(gameObject.transform.position - point).magnitude}");
            force = transform.forward * speed * actions.rightTrackSpeed;
            point = transform.TransformPoint(Vector3.right * tankWidth * 0.5f/* + Vector3.down * goCenterHeight*/);
            rb.AddForceAtPosition(force, point, ForceMode.Force);
            //Debug.Log($"Force: {force.magnitude}; dist: {(gameObject.transform.position - point).magnitude}");
            //Debug.LogWarning("Hell!");
        }
    }



    #region Deprecated
    //private void Movement()
    //{
    //    var difference = Mathf.Abs(leftTrackSpeed - rightTrackSpeed);
    //    float translation = Mathf.Abs(leftTrackSpeed) > Mathf.Abs(rightTrackSpeed) ? leftTrackSpeed : rightTrackSpeed;
    //    translation -= difference * 0.5f * (translation < 0f ? -1f : 1f);

    //    float alpha = Mathf.Atan(difference * speed / tankWidth) * Mathf.Rad2Deg;
    //    //var beta = Mathf.Atan()
    //    //rb.ang
    //    if (rightTrackSpeed > leftTrackSpeed)
    //        alpha *= -1;

    //    //transform.Translate(transform.forward * translation * Time.deltaTime * speed, Space.World);
    //    rb.AddForce(transform.forward * translation * (speed), ForceMode.Force);
    //    rb.AddTorque(Quaternion.AngleAxis(alpha, transform.up).eulerAngles, ForceMode.Force);
    //    //transform.Rotate(new Vector3(0f, alpha * Time.deltaTime));
    //}

    //private void KinematicUpdate()
    //{
    //    var difference = Mathf.Abs(leftTrackSpeed - rightTrackSpeed);
    //    float translation = Mathf.Abs(leftTrackSpeed) > Mathf.Abs(rightTrackSpeed) ? leftTrackSpeed : rightTrackSpeed;
    //    translation -= difference * 0.5f * (translation < 0f ? -1f : 1f);

    //    float alpha = Mathf.Atan(difference * speed / tankWidth) * Mathf.Rad2Deg;
    //    if (rightTrackSpeed > leftTrackSpeed)
    //        alpha *= -1;

    //    transform.Translate(transform.forward * translation * Time.deltaTime * speed, Space.World);
    //    transform.Rotate(new Vector3(0f, alpha * Time.deltaTime));
    //}
    #endregion
}
