using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Jurassic;



[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(DamagableBehaviour))]
public class Tank : MonoBehaviour, PoliticsSubject
{

    public bool execute = false;
    [Header("Parts")]
    public string turretGameObjectName = "Turret";
    public string radarGameObjectName = "Radar";
    public float radarRadius = 4f;
    public string muzzleGameObjectName = "Muzzle";
    public GameObject impactPrefab;
   

    [Header("Actuators")]
    public float speed = 5f;
    public float tankWidth = 2f;
    public float rotationTreshold = 0.05f;
    public float vertialForceDisplacement = -0.5f;
    public float turretAngularSpeed = 20f;
    public float radarAngularSpeed = 60f;
    [Range(0f,1f)]
    public float heatPerShot = 0.2f; //heat treshold = 1f
    public float overheatFine = 0.2f;
    [Range(0f,1f)]
    public float coolingRate = 0.5f;
    public float firePeriod = 1f;
    public float impactDestructionDelay = 6f;
    public Damage damage;
    public int sideIdentifier = 0;

    public Actions actions;


    public int SideId() { return sideIdentifier; }

    protected ScriptEngine engine = new ScriptEngine();
    protected float initTime;
    protected float radarAzimuth = 0f;
    protected Logger logger = new DummyLogger();
    protected Rigidbody rb;
    protected DamagableBehaviour hb;
    protected Sensors sensors = new Sensors();
    protected CompiledScript compiledCode;

    private Transform turret;
    private ParticleSystem muzzleFlash;
    private Transform radar; 

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody>();
        hb = GetComponent<DamagableBehaviour>();

        System.Action<string> logger = this.logger.Log;
        engine.SetGlobalFunction("log", logger);

        engine.EnableExposedClrTypes = true;
        engine.SetGlobalValue("actions", actions);
        engine.SetGlobalValue("sensors", sensors);

        initTime = Time.time;
        turret = transform.Find(turretGameObjectName);
        //muzzleFlash = transform.Find(muzzleGameObjectName).GetComponent<ParticleSystem>();
        //muzzleFlash = GetComponentInChildren<ParticleSystem>();
        muzzleFlash = turret.Find(muzzleGameObjectName).GetComponent<ParticleSystem>();
        radar = transform.Find(radarGameObjectName);
    }

    private void FixedUpdate()
    {
        UpdateSensorData();

        if (execute)
            Execute();
        ValidateActions();

        ApplyMovement();
        ApplyTurretRotation();
        ApplyRadarRotation();
        ApplyFire();
    }


    #region CodeMethods
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
            catch (Jurassic.Compiler.SyntaxErrorException e)
            {
                logger.Log($"JavaScript syntax error has occured at line {e.LineNumber} with message: {e.Message}");
                compiledCode = null;
            }
        }
    }
    public virtual void Execute()
    {
        if (compiledCode != null)
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
    #endregion

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
            
            force = transform.forward * speed * actions.rightTrackCoef;
            point = transform.TransformPoint(Vector3.right * tankWidth * 0.5f + Vector3.up * vertialForceDisplacement);
            rb.AddForceAtPosition(force, point, ForceMode.Force);
        }
    }

    private void ApplyTurretRotation()
    {
        turret.Rotate(Vector3.up * turretAngularSpeed * actions.turretAngularCoef * Time.fixedDeltaTime, Space.Self);
    }
    private void ApplyRadarRotation()
    {
        radarAzimuth = (radarAzimuth + radarAngularSpeed * Time.fixedDeltaTime * actions.radarAngularCoef) % 360;
        radar.localRotation = Quaternion.Euler(Vector3.up*radarAzimuth);
        radar.localPosition = new Vector3(Mathf.Sin(radarAzimuth*Mathf.Deg2Rad) * radarRadius, radar.localPosition.y, Mathf.Cos(radarAzimuth* Mathf.Deg2Rad) * radarRadius);
    }

    private float heat;
    private float nextTimeToFire;
    private void ApplyFire()
    {
        heat -= coolingRate;
        if (heat < 0f)
            heat = 0f;

        if (heat > 1f || actions.fireShots <= 0f || Time.time < nextTimeToFire)
            return;

        actions.fireShots--;
        nextTimeToFire = Time.time + firePeriod;
        heat += heatPerShot;
        if (heat >= 1f)
            heat += overheatFine;

        if (muzzleFlash.isPlaying)
            muzzleFlash.Stop();
        muzzleFlash.Play();

        RaycastHit rhi;
        Ray ray = new Ray(muzzleFlash.transform.position, muzzleFlash.transform.forward);
        if(Physics.Raycast(ray, out rhi, float.PositiveInfinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            var impact = Instantiate(impactPrefab);
            impact.transform.SetPositionAndRotation(rhi.point, muzzleFlash.transform.rotation);
            if (impactDestructionDelay >= 0f)
                Destroy(impact, impactDestructionDelay);

            var hc = rhi.collider.gameObject.GetComponent<HealthCare>();
            if(hc!=null)
            {
                hc.ReceiveDamage(damage);
            }
        }

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
        sensors.health01 = hb.Health01();

        sensors.radar.relativeAzimuth = radarAzimuth;
        RaycastHit rhi;
        var radarDirection = new Vector3(Mathf.Sin(radarAzimuth), 0f, Mathf.Cos(radarAzimuth));
        var radarRay = new Ray(transform.position, radarDirection);
        sensors.radar.distance = float.PositiveInfinity;
        sensors.radar.category = Sensors.Radar.Category.Ground;
        if (Physics.Raycast(radarRay, out rhi, float.PositiveInfinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            sensors.radar.distance = rhi.distance;
            var politican = rhi.collider.gameObject.GetComponent<PoliticsSubject>();
            if(politican!=null)
            {
                if (politican.SideId() != SideId())
                {
                    sensors.radar.category = Sensors.Radar.Category.Enemy;
                }
                else sensors.radar.category = Sensors.Radar.Category.Ally;
            }
        }
        
        
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
    #region NestedClasses&Interfaces
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
        public double health01;
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
    #endregion
}
