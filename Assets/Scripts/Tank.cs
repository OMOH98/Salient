using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Jurassic;
using Jurassic.Library;



[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(DamagableBehaviour))]
public class Tank : MonoBehaviour, PoliticsSubject
{

    public bool execute = false;
    [Header("Parts")]
    public string turretGameObjectName = "Turret";
    public string radarGameObjectName = "Radar";
    public float radarRadius = 4f;
    public float radarHeight = 0.5f;
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
    public DamagableBehaviour healthCare { get; private set; }

    protected ScriptEngine engine = new ScriptEngine();
    protected float initTime;
    protected float radarAzimuth = 0f;
    protected Logger logger = null;
    protected Rigidbody rb;

    protected Sensors sensors = new Sensors();
    protected CompiledScript compiledCode;

    private Transform turret;
    private ParticleSystem muzzleFlash;
    private Transform radar; 



    protected virtual void Start()
    {
        StaticStart();
        if (logger == null)
            StartScripting(new DummyLogger());
    }
    protected void StaticStart()
    {
        rb = GetComponent<Rigidbody>();
        healthCare = GetComponent<DamagableBehaviour>();

        turret = transform.Find(turretGameObjectName);
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
    public const string loopFunction = "loop";
    public const string setupFunction = "setup";
    public const string logFunction = "log";
    
    public string code
    {
        set
        {
            compiledCode = Compile(value);
            if (compiledCode != null)
            {
                try
                {
                    compiledCode.Execute(engine);
                }
                catch (JavaScriptException e)
                {
                    logger.Log($"JavaScript error has occured at line {e.LineNumber} with message: {e.Message}");
                }
            }
        }
    }
    
    protected CompiledScript Compile(string value)
    {
        CompiledScript ret = null;
        try
        {
            var source = new StringScriptSource(value);
            ret = engine.Compile(source);
        }
        catch (JavaScriptException e)
        {
            logger.Log($"JavaScript error has occured at line {e.LineNumber} with message: {e.Message}");
        }
        catch (Jurassic.Compiler.SyntaxErrorException e)
        {
            logger.Log($"JavaScript syntax error has occured at line {e.LineNumber} with message: {e.Message}");
        }
        return ret;
    }
    protected void ExecuteOnce(string code)
    {
        var cmp = Compile(code);
        if (cmp != null)
            cmp.Execute(engine);
    }
    protected void CallGlobalAction(string name)
    {
        if (compiledCode != null)
        {
            try
            {
                engine.CallGlobalFunction(name);
            }
            catch (JavaScriptException e)
            {
                logger.Log($"JavaScript error has occured at line {e.LineNumber} with message: {e.Message}");
            }
        }
    }
    public virtual void Execute()
    {
        CallGlobalAction(loopFunction);
    }
    public void StartScripting(Logger lgr)
    {
        logger = lgr;
        //engine.EnableExposedClrTypes = true;
        RestartScripting();
    }
    public void RestartScripting()
    {
        System.Action<string> logger = this.logger.Log;
        engine.SetGlobalFunction(logFunction, logger);
        engine.SetGlobalValue(nameof(actions), actions);
        //engine.SetGlobalValue(nameof(actions), actions);
        //engine.SetGlobalValue(nameof(sensors), sensors);

        CallGlobalAction(setupFunction);
        initTime = Time.time;
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
        var radarDirection = new Vector3(Mathf.Sin(radarAzimuth * Mathf.Deg2Rad), 0f, Mathf.Cos(radarAzimuth * Mathf.Deg2Rad)).normalized;
        radarDirection = transform.TransformDirection(radarDirection);
        //var radarHeight = radar.position.y - turret.position.y;
        radar.position = turret.position + Vector3.up * radarHeight + radarDirection * radarRadius;
        //radar.localPosition = new Vector3(Mathf.Sin(radarAzimuth*Mathf.Deg2Rad) * radarRadius, radar.localPosition.y, Mathf.Cos(radarAzimuth* Mathf.Deg2Rad) * radarRadius);
    }

    public float heat { get; private set; }
    private float nextTimeToFire;
    private void ApplyFire()
    {
        heat -= coolingRate*Time.fixedDeltaTime;
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

    private void UpdateSensorData()
    {
        sensors.azimuth = transform.rotation.eulerAngles.y % 360;
        sensors.time = Time.time - initTime;
        sensors.health01 = healthCare.Health01();

        sensors.turret.heat01 = heat;
        sensors.turret.relativeAzimuth = turret.localRotation.eulerAngles.y;

        sensors.radar.relativeAzimuth = radarAzimuth;
        RaycastHit rhi;
        var radarDirection = new Vector3(Mathf.Sin(radarAzimuth*Mathf.Deg2Rad), 0f, Mathf.Cos(radarAzimuth * Mathf.Deg2Rad));
        radarDirection = transform.TransformDirection(radarDirection);
        var radarRay = new Ray(turret.position, radarDirection);
        sensors.radar.distance = float.PositiveInfinity;
        sensors.radar.category = Sensors.Radar.Category.Ground;
        if (Physics.Raycast(radarRay, out rhi, float.PositiveInfinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            Debug.DrawLine(radarRay.origin, rhi.point, Color.red);
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
    public class Actions:ObjectInstance
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

        public new object this[object Key]
        {
            get
            {
                foreach (var item in typeof(Actions).GetFields().Where(fi => fi.IsPublic))
                {
                    if (item.Name == Key.ToString())
                    {
                        return item.GetValue(this);
                    }
                }
                return base[Key];
            }
            set
            {
                foreach (var item in typeof(Actions).GetFields().Where(fi => fi.IsPublic))
                {
                    if (item.Name == Key.ToString())
                    {
                        item.SetValue(this, value);
                        return;
                    }
                }
                base[Key] = value;
            }
        }
        public Actions(ScriptEngine engine) : base(engine)
        {

        }
    }

    [System.Serializable]
    public class Sensors
    {
        public double azimuth;
        public double time;
        public double health01;
        public Radar radar;
        public Turret turret;

        public double radarAbsoluteAzimuth
        {
            get
            {
                return (azimuth + radar.relativeAzimuth) % 360f;
            }
        }
        public double turretAbsoluteAzimuth
        {
            get
            {
                return (azimuth + turret.relativeAzimuth) % 360f;
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
        public class Turret
        {
            public double relativeAzimuth;
            public double heat01;
            public bool readyToFire
            {
                get { return heat01 <= 1f; }
            }
        }

        public Sensors()
        {
            radar = new Radar();
            turret = new Turret();
<<<<<<< HEAD
            proxor = new List<Radar>();
            recentDamage = new Queue<RecentDamage>();
            
=======
>>>>>>> parent of 0054e4c... Broken: new sensors are not working
        }
    }

    public interface Logger
    {
        void Log(string msg);
    }
    public class DummyLogger : Logger
    {
        public void Log(string msg)
        {
            //Debug.Log(msg);
            ;
        }
    }
    #endregion
}
