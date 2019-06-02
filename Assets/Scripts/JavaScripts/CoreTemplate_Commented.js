//This is a commented template document that does nothing.

//It is written to let you know about possibilities
//of tank programming interface.

//Change implementations setup, loop and other functions
//in order to add functionality (see below).

/**
 * @param {String} msg 
 */
function log(msg){}

var actions = {
    leftTrackCoef: 0, //coefficient for speed of the left track. 
    //Values from -0.5 to 1 are acceptable.

    rightTrackCoef: 0, //coefficient for speed of the right track. 
    //Values from -0.5 to 1 are acceptable.

    turretAngularCoef: 0, //coefficient for angular speed of the turret. 
    //Values from -1 to 1 are acceptable.

    radarAngularCoef: 0, //coefficient for angular speed of the radar. 
    //Values from -1 to 1 are acceptable.
    
    fireShots: 0 //quantity of shots to perform. 
    //Could be set to any number, including Number.POSITIVE_INFINITY.
}

var sensors = {
    azimuth : 0,                //global azimuth (relative to North) from 0 to 360
                                //positive azimuth is clockwise
    time : 0,                   //seconds passed since initialization
    health01 : 0,               //health percentage from 0 to 1
    angularVelocity : 0,        //degrees per second. Positive is clockwise.
    velocity:{
        x:0, y:0, z:0           //meters per second
    },
    radar : {
        distance : 0,           //distance to object hit by radar ray
        relativeAzimuth : 0,    //azimuth to that object relative to tanks' hull;
        categoryIndex : 0,      //1 for Ground; 2 for Enemy; 3 for Ally;

        category : "",          //"Ground", "Enemy" or "Ally"
    },
    turret : {
        relativeAzimuth : 0,
        heat01 : 0,             //heat from 0 to 1. 
                                //If heat is >1, weapon can not operate until it cools.
        readyToFire : false,    //is weapon heat less or equal 1
    },

    radarAbsoluteAzimuth: 0, 
    turretAbsoluteAzimuth: 0,
}

var stats = {
    engineForce : 0,            /*How much force is applied by each engine 
    (there are two of them)*/
    tankWidth : 0,
    rotationTreshold : 0, /*what difference should be betwheen track coefs'
    in order to apply any torque to robot*/

    turretAngularSpeed : 0, /*turret angular speed in degrees per second
    when turretAngularCoef = 1*/
    radarAngularSpeed : 0, /*same as previous for radar*/
    proxorRadius : 0, /*radius of proximity sensor recation*/
    heatPerShot : 0, //how weapon heats on single shot
    overheatFine : 0, //how much additional heat added on overheat
    coolingRate : 0, //how much heat lowers over one second
    firePeriod : 0, //minimum time that passes between shots
    damage:{
        ammount: 0 //ammount of damage inflicted by single weapon shot
    }
}


//called once when robot is loading its scripting system
function setup(){
	log("setting up");
}

//called repeatedly every cycle of physics update (approximatly every 0.02 seconds)
function loop(){
	
}

/**
 * @param {Number} ammount
 * @param {Number} relativeAzimuthToSource
 */
function onDamageTaken(ammount, relativeAzimuthToSource){
	
}

/**
 * @param {Number} distance
 * @param {Number} relativeAzimuth
 * @param {String} category
 */
function onProximityEnter(distance, relativeAzimuth, category){
	
}

/**
 * @param {Number} distance
 * @param {Number} relativeAzimuth
 * @param {String} category
 */
function onCollisionEnter(distance, relativeAzimuth, category){
	
}