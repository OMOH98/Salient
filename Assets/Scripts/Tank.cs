using System.Collections;
using System.Collections.Generic;
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
    public float proxorUpdatePeriod = 2f;
    public float proxorRadius = 10f;
    public int proxorMaxCount = 16;
    public int recentDamagesMemory = 16;
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
            var recentd = new Sensors.RecentDamage() { ammount = d.ammount, timestamp = sensors.time, sourceAzimuth = (alpha + 180f) % 360f };
            sensors.recentDamage.Enqueue(recentd);
            while (sensors.recentDamage.Count > recentDamagesMemory)
                sensors.recentDamage.Dequeue();
        });

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
    public void Expose(Exposable exposable)
    {
        var n = exposable.GetType().Name.ToLower();
        engine.SetGlobalValue(n, exposable.GetMirror());
    }
    public void RestartScripting()
    {
        System.Action<string> logger = this.logger.Log;
        engine.SetGlobalFunction(logFunction, logger);
        Expose(actions);
        //engine.SetGlobalValue(nameof(actions), actions);
        //engine.SetGlobalValue(nameof(sensors), sensors);

        CallGlobalAction(setupFunction);
        initTime = Time.time;
    }
    #endregion

    #region ApplyMethods
    private void ApplyMovement()
    {
        if (grounded)
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
                var d = new Damage() {ammount = damage.ammount, direction = muzzleFlash.transform.forward };
                hc.ReceiveDamage(d);
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

    float nextTimeToUpdateProxor = 0f;
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
        if (Physics.Raycast(radarRay, out rhi, float.PositiveInfinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            Debug.DrawLine(radarRay.origin, rhi.point, Color.red);
            sensors.radar.distance = rhi.distance;
            sensors.radar.category = Sensors.Radar.Categorize(rhi.collider.gameObject, SideId());
        }

        if (Time.time >= nextTimeToUpdateProxor)
        {
            nextTimeToUpdateProxor = Time.time + proxorUpdatePeriod;
            sensors.lastProxorUpdate = sensors.time;

            var po = Physics.OverlapSphere(turret.position, proxorRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            sensors.proxor.Clear();
            foreach (var col in po)
            {
                var cat = Sensors.Radar.Categorize(col.gameObject, SideId());
                Vector3 center = transform.InverseTransformPoint(col.gameObject.transform.position);
                var dist = center.magnitude;
                center.y = 0f;
                var alpha = Vector3.Angle(Vector3.forward, center);
                if (center.x < 0f)
                    alpha *= -1f;

                var rep = new Sensors.Radar(dist, alpha % 360, cat);
                sensors.proxor.Add(rep);

                if (sensors.proxor.Count >= proxorMaxCount)
                    break;
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
    public abstract class Exposable
    {
        private ObjectInstance mirror;
        public ObjectInstance GetMirror()
        {
            RefreshMirror();
            return mirror;
        }
        public void RefreshMirror()
        {
            foreach (var item in this.GetType().GetFields())
            {
                if(item.IsPublic)
                {
                    mirror[item.Name] = item.GetValue(this);
                }
            }
        }
        public Exposable(ScriptEngine e)
        {
            mirror = e.Object.Construct();
        }
    }

    [System.Serializable]
    public class Actions:Exposable
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
        public Actions(ScriptEngine e):base(e)
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
        public List<Radar> proxor;
        public double lastProxorUpdate;
        public Queue<RecentDamage> recentDamage;

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

            public Radar()
            {

            }
            public Radar(double _distance, double _relativeAzimuth, Category _category)
            {
                distance = _distance;
                relativeAzimuth = _relativeAzimuth;
                category = _category;
            }


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
        public struct RecentDamage
        {
            public double ammount;
            public double sourceAzimuth;
            public double timestamp;
        }

        public Sensors()
        {
            radar = new Radar();
            turret = new Turret();
            proxor = new List<Radar>();
            recentDamage = new Queue<RecentDamage>();
            
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
