/**
 * @param {String} msg 
 */
function log(msg) { }

var actions = {
    leftTrackCoef: 0,
    rightTrackCoef: 0,
    turretAngularCoef: 0,
    radarAngularCoef: 0,
    fireShots: 0
}

var sensors = {
    azimuth: 0,
    time: 0,
    health01: 0,
    angularVelocity: 0,
    velocity: {
        x: 0, y: 0, z: 0
    },
    radar: {
        distance: 0,
        relativeAzimuth: 0,
        categoryIndex: 0,

        category: "",
    },
    turret: {
        relativeAzimuth: 0,
        heat01: 0,
        readyToFire: false,
    },

    radarAbsoluteAzimuth: 0,
    turretAbsoluteAzimuth: 0,
}

var stats = {
    engineForce: 0,
    tankWidth: 0,
    rotationTreshold: 0,

    turretAngularSpeed: 0,
    radarAngularSpeed: 0,
    proxorRadius: 0,

    heatPerShot: 0, //heat treshold = 1f
    overheatFine: 0,
    coolingRate: 0,
    firePeriod: 0,
    damage: {
        ammount: 0
    }
}



var radarCoef = 0;

function setup() {
    log("setting up");
    radarCoef = stats.turretAngularSpeed / stats.radarAngularSpeed;
}

var state = 0;
var wasHit = false;
var timeToEvade = 0;
var TIME_TO_EVADE = 2;
var collided = false;
var timeToCollide = 0;
var TIME_TO_COLLIDE = 3;
var shots = 0;
var directions = 1;
var ang = 0;
var loop = function () {
    var newState = state;
    ang = sensors.angularVelocity;
    while (ang > 180)
        ang -= 360;
    do {
        state = newState;
        switch (newState) {
            case 0://search

                if (collided) {
                    timeToCollide = sensors.time + TIME_TO_COLLIDE;
                    newState = 3;
                }
                else if (wasHit) {
                    timeToEvade = sensors.time + TIME_TO_EVADE;
                    newState = 2;
                    shots = 0;
                }
                else if (sensors.radar.categoryIndex == 2) {
                    newState = 4;
                    break;
                }
                actions.leftTrackCoef = 0.22;
                actions.rightTrackCoef = actions.leftTrackCoef - 0.08 * directions;
                actions.turretAngularCoef = 1 * directions;
                actions.radarAngularCoef = radarCoef * directions;
                break;
            case 1: //firing
                actions.leftTrackCoef = actions.rightTrackCoef = actions.turretAngularCoef = actions.radarAngularCoef = 0;
                actions.fireShots = 1;

                if (collided) {
                    timeToCollide = sensors.time + TIME_TO_COLLIDE;
                    newState = 3;
                }
                else if (wasHit) {
                    timeToEvade = sensors.time + TIME_TO_EVADE;
                    newState = 2;
                    shots = 0;
                } else if (sensors.radar.categoryIndex != 2) {
                    actions.fireShots = 0;
                    newState = 0;
                    if (Math.sign(ang) == Math.sign(directions))
                        directions *= -1;
                }
                break;
            case 2://evading
                actions.rightTrackCoef = actions.leftTrackCoef = 1;  
                if(directions<0){
                    actions.leftTrackCoef -=0.2;
                }else   {
                    actions.rightTrackCoef -=0.2;
                }

                if (wasHit) {
                    shots += 1;
                    wasHit = false;
                    timeToEvade = sensors.time + TIME_TO_EVADE * 0.5;
                }
                
                if (shots > 3) {
                    timeToCollide = sensors.time + TIME_TO_EVADE;
                    newState = 3;
                }
                else if (collided) {
                    timeToCollide = sensors.time + TIME_TO_COLLIDE;
                    newState = 3;
                }
                else if (sensors.time >= timeToEvade) {
                    newState = 0;
                }
                break;
            case 3://on collission
                collided = false;
                actions.leftTrackCoef = actions.rightTrackCoef = -0.5;
                if (sensors.time >= timeToCollide) {
                    newState = 0;
                }
                break;
            case 4://braking
                if (collided) {
                    timeToCollide = sensors.time + TIME_TO_COLLIDE;
                    newState = 3;
                }
                else if (wasHit) {
                    timeToEvade = sensors.time + TIME_TO_EVADE;
                    newState = 2;
                    shots = 0;
                    wasHit = false;
                } else if (Math.abs(ang) >= 5) {

                    actions.leftTrackCoef = -0.1;
                    actions.rightTrackCoef = 0.1;
                    if (ang < 0) {
                        actions.leftTrackCoef *= -1;
                        actions.rightTrackCoef *= -1;
                    }
                }
                else if (Math.abs(sensors.velocity.z) > 10) {

                    actions.leftTrackCoef = actions.rightTrackCoef = -0.2
                    if (sensors.velocity.z < 0) {
                        actions.leftTrackCoef *= -1;
                        actions.rightTrackCoef *= -1;
                    }
                }
                else {
                    newState = 1;
                }

                break;
            default:
                break;
        }
        if (newState != state)
            log("NewState: " + newState);
    } while (newState != state);
}

/**
 * @param {Number} ammount ammount of damage
 * @param {Number} relativeAzimuthToSource
 */
function onDamageTaken(ammount, relativeAzimuthToSource) {
    wasHit = true;
}

/**
 * @param {Number} distance
 * @param {Number} relativeAzimuth
 * @param {String} category
 */
function onProximityEnter(distance, relativeAzimuth, category) {

}

/**
 * @param {Number} distance
 * @param {Number} relativeAzimuth
 * @param {String} category
 */
function onCollisionEnter(distance, relativeAzimuth, category) {

}