using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Jurassic;



[RequireComponent(typeof(Rigidbody))]
public class Tank : MonoBehaviour
{

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

    public string code
    {
        set
        {
            try
            {
                var source = new StringScriptSource(value);
                compiledCode = engine.Compile(source);
            }
            catch (JavaScriptException e)
            {
                logger.Log($"JavaScript error has occured at line {e.LineNumber} with message: {e.Message}");
                compiledCode = null;
            }            
        }
    }


    protected ScriptEngine engine = new ScriptEngine();
    protected float initTime;
    protected float radarAzimuth = 0f;
    protected Logger logger = new DummyLogger();
    protected Rigidbody rb;
    protected Sensors sensors = new Sensors();
    protected CompiledScript compiledCode;

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
        public double azimuth;
        public double time;
        public Radar radar;
        public double radarAbsoluteAzimuth
        {
            get
            {
                return (azimuth + radar.relativeAzimuth) % 360f;
            }
        }

        public class Radar
        {
            public double distance;
            public double relativeAzimuth;
            public Category category;
            public string catString
            {
                get
                {
                    return System.Enum.GetName(typeof(Category), category);
                }
            }
            public enum Category
            {
                Wall, Ground, Enemy, Ally, Neutral
            }
        }
        public Sensors()
        {
            radar = new Radar();
        }
    }

    public interface Logger
    {
        void Log(string msg);
    }
    private class DummyLogger : Logger
    {
        public void Log(string msg)
        {
            Debug.Log(msg);
        }
    }





    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody>();

        System.Action<string> logger = this.logger.Log;
        engine.SetGlobalFunction("log", logger);

        engine.EnableExposedClrTypes = true;
        engine.SetGlobalValue("actions", actions);
        engine.SetGlobalValue("sensors", sensors);

        initTime = Time.time;
        turret = transform.Find(turretGameObjectName);
    }

    private void FixedUpdate()
    {
        //engine.SetGlobalValue("sensors", );

        //TODO: Validation of actions values;
        //actions.Validate();
        UpdateSensorData();

        if (execute)
            Execute();

        ApplyMovement();
        ApplyTurretRotation();
        ApplyRadarRotation();
    }

    public virtual void Execute()
    {
        if(compiledCode!=null)
        {
            try
            {
                compiledCode.Execute(engine);
            }
            catch (JavaScriptException e)
            {
                logger.Log($"JavaScript error has occured at line {e.LineNumber} with message: {e.Message}");
                throw;
            }
           
        }
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
        turret.Rotate(Vector3.up * turretAngularSpeed * actions.turretAngularCoef * Time.fixedDeltaTime, Space.Self);
    }
    private void ApplyRadarRotation()
    {
        radarAzimuth = (radarAzimuth + radarAngularSpeed * Time.fixedDeltaTime * actions.radarAngularCoef) % 360;
    }
    #endregion
    #region TankAPIFunctions
    private void SetTrackCoef(float value, bool left)
    {
        if (value > 1f || value < -0.5f)
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
        if (Mathf.Abs(value) > 1f)
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

    private void UpdateSensorData()
    {
        sensors.azimuth = transform.rotation.eulerAngles.y % 360;
        sensors.time = Time.time - initTime;
        sensors.radar.relativeAzimuth = radarAzimuth;
        //TODO:implement dummies
        sensors.radar.distance = Time.time % 100f;
        sensors.radar.category = Sensors.Radar.Category.Enemy;
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

}
