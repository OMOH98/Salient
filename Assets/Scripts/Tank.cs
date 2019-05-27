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
    public Stats stats;
    public float vertialForceDisplacement = -0.5f;
    public float impactDestructionDelay = 6f;
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
    private SphereCollider proxor;
    private ObjectInstance statsMirror;



    protected virtual void Start()
    {
        if (logger == null)
            StartScripting(new DummyLogger());
        StaticStart();
    }
    protected void StaticStart()
    {
        rb = GetComponent<Rigidbody>();
        healthCare = GetComponent<DamagableBehaviour>();
        healthCare.onDamageTaken.AddListener((d) =>
        {
            var direction = transform.InverseTransformDirection(d.direction);
            var alpha = Vector3.Angle(Vector3.forward, direction);
            if (direction.x < 0f)
                alpha *= -1f;
            CallGlobalFunction(onDamageTaken, (double)d.ammount, (alpha + 900d) % 360d);
        });


        turret = transform.Find(turretGameObjectName);
        muzzleFlash = turret.Find(muzzleGameObjectName).GetComponent<ParticleSystem>();
        radar = transform.Find(radarGameObjectName);

        proxor = gameObject.AddComponent<SphereCollider>();
        proxor.isTrigger = true;
        proxor.center = turret.localPosition;
        proxor.radius = stats.proxorRadius;
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
    #region CollisionSensors
    bool grounded = false;
    List<GameObject> collidingObjects = new List<GameObject>();
    private void OnCollisionEnter(Collision collision)
    {
        var pol = collision.gameObject.GetComponent<PoliticsSubject>();
        if(pol==null)
        {
            grounded = true;
        }
        collidingObjects.Add(collision.gameObject);

        var info = Sensors.Radar.GetRadarInfo(collision.gameObject, this);

        CallGlobalFunction(onCollisionEnter, info.distance, info.relativeAzimuth, info.category);
    }
    
    private void OnCollisionExit(Collision collision)
    {
        if(collidingObjects.Remove(collision.gameObject))
        {
            var pol = collision.gameObject.GetComponent<PoliticsSubject>();
            if (pol == null)
            {
                var g = false;
                foreach (var item in collidingObjects)
                {
                    pol = item.GetComponent<PoliticsSubject>();
                    if(pol==null)
                    {
                        g = true;
                        break;
                    }
                }
                grounded = g;
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        var info = Sensors.Radar.GetRadarInfo(other.gameObject, this);
        CallGlobalFunction(onProximityEnter, info.distance, info.relativeAzimuth, info.category);
    }
    #endregion
    #region CodeMethods
    public const string loopFunction = "loop";
    public const string setupFunction = "setup";
    public const string logFunction = "log";
    public const string onDamageTaken = nameof(onDamageTaken);
    public const string onProximityEnter = nameof(onProximityEnter);
    public const string onCollisionEnter = nameof(onCollisionEnter);
    
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
    protected void CallGlobalFunction(string name, params object[] args)
    {
        if (compiledCode != null)
        {
            try
            {
                engine.CallGlobalFunction(name, args);
            }
            catch (JavaScriptException e)
            {
                logger.Log($"JavaScript error has occured at line {e.LineNumber} with message: {e.Message}");
            }
            catch (System.InvalidOperationException/*e*/)// function Name is not defined in tank script
            {
                ;//Debug.Log(e);
            }
        }
    }
    public virtual void Execute()
    {
        CallGlobalFunction(loopFunction);
    }
    public void StartScripting(Logger lgr)
    {
        logger = lgr;
        engine.EnableExposedClrTypes = true;
        RestartScripting();
    }
    public void RestartScripting()
    {
        System.Action<string> logger = this.logger.Log;

        engine.SetGlobalFunction(logFunction, logger);
        engine.SetGlobalValue(nameof(actions), actions);
        engine.SetGlobalValue(nameof(sensors), sensors);
        statsMirror = engine.Object.Construct();
        stats.PushTo(statsMirror);
        engine.SetGlobalValue(nameof(stats), statsMirror);

        CallGlobalFunction(setupFunction);
        initTime = Time.time;
    }
    #endregion

    #region ApplyMethods
    private void ApplyMovement()
    {
        if (grounded)
        {
            if (Mathf.Abs(actions.leftTrackCoef - actions.rightTrackCoef) < stats.rotationTreshold)
            {
                rb.AddForce(transform.forward * 2 * stats.engineForce * Mathf.Min(actions.leftTrackCoef, actions.rightTrackCoef));
            }
            else
            {
                var force = transform.forward * stats.engineForce * actions.leftTrackCoef;
                var point = transform.TransformPoint(Vector3.left * stats.tankWidth * 0.5f + Vector3.up * vertialForceDisplacement);
                rb.AddForceAtPosition(force, point, ForceMode.Force);

                force = transform.forward * stats.engineForce * actions.rightTrackCoef;
                point = transform.TransformPoint(Vector3.right * stats.tankWidth * 0.5f + Vector3.up * vertialForceDisplacement);
                rb.AddForceAtPosition(force, point, ForceMode.Force);
            }
        }
    }

    private void ApplyTurretRotation()
    {
        turret.Rotate(Vector3.up * stats.turretAngularSpeed * actions.turretAngularCoef * Time.fixedDeltaTime, Space.Self);
    }

    private void ApplyRadarRotation()
    {
        radarAzimuth = (radarAzimuth + stats.radarAngularSpeed * Time.fixedDeltaTime * actions.radarAngularCoef + 720f) % 360;
        radar.localRotation = Quaternion.Euler(Vector3.up*radarAzimuth);
        var radarDirection = new Vector3(Mathf.Sin(radarAzimuth * Mathf.Deg2Rad), 0f, Mathf.Cos(radarAzimuth * Mathf.Deg2Rad)).normalized;
        radarDirection = transform.TransformDirection(radarDirection);
        radar.position = turret.position + Vector3.up * radarHeight + radarDirection * radarRadius;
    }

    public float heat { get; private set; }
    private float nextTimeToFire;
    private void ApplyFire()
    {
        heat -= stats.coolingRate*Time.fixedDeltaTime;
        if (heat < 0f)
            heat = 0f;

        if (heat > 1f || actions.fireShots <= 0f || Time.time < nextTimeToFire)
            return;

        actions.fireShots--;
        nextTimeToFire = Time.time + stats.firePeriod;
        heat += stats.heatPerShot;
        if (heat >= 1f)
            heat += stats.overheatFine;

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
                var d = new Damage() {ammount = stats.damage.ammount, direction = muzzleFlash.transform.forward };
                hc.ReceiveDamage(d);
            }
        }

    }
    
    #endregion
    #region TankAPIFunctions
    
    private void UpdateSensorData()
    {
        sensors.azimuth = (transform.rotation.eulerAngles.y + 720f) % 360f;
        sensors.time = Time.time - initTime;
        sensors.health01 = healthCare.Health01();

        sensors.angularVelocity = rb.angularVelocity.y * Mathf.Rad2Deg;
        sensors.velocity = transform.TransformDirection(rb.velocity);

        sensors.turret.heat01 = heat;
        sensors.turret.relativeAzimuth = (turret.localRotation.eulerAngles.y+720f)%360f;

        sensors.radar.relativeAzimuth = radarAzimuth;
        RaycastHit rhi;
        var radarDirection = new Vector3(Mathf.Sin(radarAzimuth*Mathf.Deg2Rad), 0f, Mathf.Cos(radarAzimuth * Mathf.Deg2Rad));
        radarDirection = transform.TransformDirection(radarDirection);
        var radarRay = new Ray(turret.position, radarDirection);
        sensors.radar.distance = float.PositiveInfinity;
        if (Physics.Raycast(radarRay, out rhi, float.PositiveInfinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            Debug.DrawLine(radarRay.origin, rhi.point, Color.red);
            sensors.radar.distance = rhi.distance;
            sensors.radar.categoryIndex = Sensors.Radar.Categorize(rhi.collider.gameObject, SideId());
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
        public double angularVelocity;
        public Vector3Double velocity;
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

        public struct Vector3Double
        {
            public double x, y, z;
            public double magnitude
            {
                get { return System.Math.Sqrt(x * x + y * y + z * z); }
            }
            public static implicit operator Vector3(Vector3Double src)
            {
                throw new System.NotImplementedException();
            }
            public static implicit operator Vector3Double(Vector3 src)
            {
                return new Vector3Double { x = src.x, y = src.y, z = src.z };
            }
        }
        public class Radar
        {
            public double distance;
            public double relativeAzimuth;
            public Category categoryIndex;

            public string category
            {
                get
                {
                    return System.Enum.GetName(typeof(Category), categoryIndex);
                }
            }
            public enum Category
            {
                Wall, Ground, Enemy, Ally, Neutral
            }
            public static Category Categorize(GameObject obj, int allyID)
            {
                var politican = obj.GetComponent<PoliticsSubject>();
                if (politican != null)
                {
                    if (politican.SideId() != allyID)
                    {
                       return Category.Enemy;
                    }
                    else return Category.Ally;
                }
                else
                {
                    return Category.Ground;
                }
            }
            public static Radar GetRadarInfo(GameObject obj, Tank reference)
            {
                var cat = Sensors.Radar.Categorize(obj, reference.SideId());
                Vector3 center = reference.transform.InverseTransformPoint(obj.transform.position);
                var dist = center.magnitude;
                center.y = 0f;
                var alpha = Vector3.Angle(Vector3.forward, center);
                if (center.x < 0f)
                    alpha *= -1f;
                return new Radar() { distance = dist, relativeAzimuth = (alpha+720f) % 360f, categoryIndex = cat };
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
        }
    }

    [System.Serializable]
    public class Stats
    {
        public float engineForce = 5f;
        public float tankWidth = 2f;
        public float rotationTreshold = 0.05f;

        public float turretAngularSpeed = 20f;
        public float radarAngularSpeed = 60f;
        public float proxorRadius = 10f;

        [Range(0f, 1f)]
        public float heatPerShot = 0.2f; //heat treshold = 1f
        public float overheatFine = 0.2f;
        [Range(0f, 1f)]
        public float coolingRate = 0.5f;
        public float firePeriod = 1f;
        public Damage damage;
        public void PushTo(ObjectInstance obj)
        {
            if (obj == null)
                throw new System.ArgumentNullException("obj must be an initialized JS object");

            var t = typeof(Stats);
            foreach (var item in t.GetFields().Where(f=>f.IsPublic&&f.FieldType == typeof(float)))
            {
                obj[item.Name] = System.Convert.ToDouble(item.GetValue(this));
            }
            obj[nameof(damage)] = (double)damage.ammount;
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
