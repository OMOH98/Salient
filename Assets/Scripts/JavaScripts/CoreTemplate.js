/**
 * @param {String} msg 
 */
function log(msg){}

var actions = {
    leftTrackCoef: 0,
    rightTrackCoef: 0,
    turretAngularCoef: 0,
    radarAngularCoef: 0,
    fireShots: 0
}

var sensors = {
    azimuth : 0,
    time : 0,
    health01 : 0,
    angularVelocity : 0,
    velocity:{
        x:0, y:0, z:0
    },
    radar : {
        distance : 0,
        relativeAzimuth : 0,
        categoryIndex : 0,

        category : "",
    },
    turret : {
        relativeAzimuth : 0,
        heat01 : 0,
        readyToFire : false,
    },

    radarAbsoluteAzimuth: 0,
    turretAbsoluteAzimuth: 0,
}

var stats = {
    engineForce : 0,
    tankWidth : 0,
    rotationTreshold : 0,

    turretAngularSpeed : 0,
    radarAngularSpeed : 0,
    proxorRadius : 0,

    heatPerShot : 0, //heat treshold = 1f
    overheatFine : 0,
    coolingRate : 0,
    firePeriod : 0,
    damage:{
        ammount: 0
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