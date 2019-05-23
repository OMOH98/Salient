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
    [Header("Parts")]
    public string turretGameObjectName = "Turret";

    [Header("Actuators")]
    public float speed = 5f;
    public float tankWidth = 2f;
    public float rotationTreshold = 0.05f;
    public float vertialForceDisplacement = -0.5f;
    public float turretAngularSpeed = 20f;
    public float radarAngularSpeed = 60f;
    public float heatPerShot = 0.2f; //heat treshold = 1f
    public float overheatFine = 0.2f;

    public Actions actions;

    private ScriptEngine engine = new ScriptEngine();
    private float initTime;
    Logger logger = new Logger();
    Rigidbody rb;
    Sensors sensors;

    [System.Serializable]
    public class Actions
    {
        [Range(-0.5f, 1f)]
        public float leftTrackCoef;
        [Range(-0.5f, 1f)]
        public float rightTrackCoef;
        [Range(-1f, 1f)]
        public float turretAngularCoef;
        [Range(-1f, 1f)]
        public float radarAngularCoef;
        public float fireShots;
    }

    [System.Serializable]
    public class Sensors
    {
        public enum ObjectCategory
        {
            Wall, Ground, Enemy, Ally, Neutral
        }
        public double azimuth;
        public double time;
    }

    public class Logger
    {
        public void Log(string msg)
        {
            Debug.Log(msg);
        }
    }

    public void ExecOnce()
    {
        try
        {
            engine.Execute(code.text);
        }
        catch (Jurassic.JavaScriptException e)
        {
            logger.Log($"JavaScript error has occured at line {e.LineNumber} with message: {e.Message}");
        }
    }



    void Start()
    {
        rb = GetComponent<Rigidbody>();

        System.Action<string> logger = this.logger.Log;
        engine.SetGlobalFunction("log", logger);

        engine.EnableExposedClrTypes = true;
        engine.SetGlobalValue("actions", actions);

        initTime = Time.time;
        turret = transform.Find(turretGameObjectName);
    }

    private void FixedUpdate()
    {
        //engine.SetGlobalValue("sensors", );
        var data = CurrentSensorData();
        var fields = typeof(Sensors).GetFields();
        foreach (var item in fields)
        {
            engine.SetGlobalValue(item.Name, item.GetValue(data));
        }


        //TODO: Validation of actions values;
        //actions.Validate();


        if (execute)
            ExecOnce();

        ApplyMovement();
        ApplyTurretRotation();
    }
    #region ApplyMethods
    private void ApplyMovement()
    {
        if (Mathf.Abs(actions.leftTrackCoef - actions.rightTrackCoef) < rotationTreshold)
        {
            rb.AddForce(transform.forward * 2 * speed * Mathf.Min(actions.leftTrackCoef, actions.rightTrackCoef));
        }
        else
        {
            var force = transform.forward * speed * actions.leftTrackCoef;
            var point = transform.TransformPoint(Vector3.left * tankWidth * 0.5f + Vector3.up * vertialForceDisplacement);
            rb.AddForceAtPosition(force, point, ForceMode.Force);
            //Debug.Log($"Force: {force.magnitude}; dist: {(gameObject.transform.position - point).magnitude}");
            force = transform.forward * speed * actions.rightTrackCoef;
            point = transform.TransformPoint(Vector3.right * tankWidth * 0.5f + Vector3.up * vertialForceDisplacement);
            rb.AddForceAtPosition(force, point, ForceMode.Force);
            //Debug.Log($"Force: {force.magnitude}; dist: {(gameObject.transform.position - point).magnitude}");
            //Debug.LogWarning("Hell!");
        }
    }

    private Transform turret;
    private void ApplyTurretRotation()
    {
        turret.Rotate(Vector3.up * turretAngularSpeed * actions.turretAngularCoef, Space.Self);
    }
    #endregion
    #region TankAPIFunctions
    private void SetTrackCoef(float value, bool left)
    {
        if(value>1f||value<-0.5f)
        {
            logger.Log($"Coefficient value for {(left == true ? "left" : "right")} track speed must be between -0.5 and 1");
        }
        var val = Mathf.Clamp(value, -0.5f, 1f);
        if (left)
        {
            actions.leftTrackCoef = val;
        }
        else actions.rightTrackCoef = val;
    }
    private void SetTurretAngularCoef(float value)
    {
        if (Mathf.Abs(value) > 1f)
            logger.Log("Coefficient value for turret angular speed must be between -1 and 1");
        actions.turretAngularCoef = Mathf.Clamp(value, -1f, 1f);
    }
    private void SetRadarAngularCoef(float value)
    {
        if(Mathf.Abs(value)>1f)
        {
            logger.Log("Coefficient value for radar angular speed must be between -1 and 1");
        }
        actions.radarAngularCoef = Mathf.Clamp(value, -1f, 1f);
    }
    private void Fire()
    {
        actions.fireShots += 1;
    }
    private void FireAtWill()
    {
        actions.fireShots = float.PositiveInfinity;
    }
    private void CeaseFire()
    {
        actions.fireShots = 0f;
    }

    private Sensors CurrentSensorData()
    {

        return new Sensors() {
            azimuth = transform.rotation.eulerAngles.y % 360,
            time = Time.time - initTime
        };
    }
    private void UpdateSensorData()
    {
        throw new System.NotImplementedException();
    }

    public bool ValidateActions()
    {
        var ret = true;
        if (actions.leftTrackCoef > 1f || actions.leftTrackCoef < -0.5f)
        {
            logger.Log("Coefficient value for left track speed must be between -0.5 and 1");
            ret = false;
            actions.leftTrackCoef = Mathf.Clamp(actions.leftTrackCoef, -0.5f, 1f);
        }

        if (actions.rightTrackCoef > 1f || actions.rightTrackCoef < -0.5f)
        {
            logger.Log("Coefficient value for right track speed must be between -0.5 and 1");
            ret = false;
            actions.rightTrackCoef = Mathf.Clamp(actions.rightTrackCoef, -0.5f, 1f);
        }

        if (Mathf.Abs(actions.radarAngularCoef) > 1f)
        {
            logger.Log("Coefficient value for radar angular speed must be between -1 and 1");
            actions.radarAngularCoef = Mathf.Clamp(actions.radarAngularCoef, -1f, 1f);
            ret = false;
        }

        if (Mathf.Abs(actions.turretAngularCoef) > 1f)
        {
            logger.Log("Coefficient value for turret angular speed must be between -1 and 1");
            actions.turretAngularCoef = Mathf.Clamp(actions.turretAngularCoef, -1f, 1f);
            ret = false;
        }
        return ret;
    }
    #endregion



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
