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
function loop() {
    var newState = state;
    do {
        state = newState;
        switch (newState) {
            case 0://search
                actions.leftTrackCoef = 0.22;
                actions.rightTrackCoef =  actions.leftTrackCoef - 0.12*directions;
                actions.turretAngularCoef = 1*directions;
                actions.radarAngularCoef = radarCoef*directions;

                if (collided) {
                    timeToCollide = sensors.time + TIME_TO_COLLIDE;
                    newState = 3;
                }
                else if (wasHit) {
                    timeToEvade = sensors.time + TIME_TO_EVADE;
                    newState = 2;
                    shots = 0;
                    wasHit = false;
                }
                else if (sensors.radar.categoryIndex == 2) {
                    newState = 4;
                }
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
                    wasHit = false;
                } else if (sensors.radar.categoryIndex != 2) {
                    actions.fireShots = 0;
                    newState = 0;
                }
                break;
            case 2://evading
                actions.leftTrackCoef = 1*directions;
                actions.rightTrackCoef = 0.8*directions;
                if (shots >= 3) {
                    timeToCollide = sensors.time + TIME_TO_EVADE;
                    newState = 3;
                }
                else if (collided) {
                    timeToCollide = sensors.time + TIME_TO_COLLIDE;
                    newState = 3;
                }
                else if (wasHit) {
                    shots += 1;
                    wasHit = false;
                    timeToEvade = sensors.time + TIME_TO_EVADE * 0.5;
                }
                else if (sensors.time >= timeToEvade) {
                    newState = 0;
                }
                break;
            case 3://on collission
                collided = false;
                actions.leftTrackCoef = actions.rightTrackCoef = -0.5*directions;
                if (sensors.time >= timeToCollide) {
                    newState = 0;
                }
            case 4://braking
                var ang = sensors.angularVelocity;
                if (collided) {
                    timeToCollide = sensors.time + TIME_TO_COLLIDE;
                    newState = 3;
                }
                else if (wasHit) {
                    timeToEvade = sensors.time + TIME_TO_EVADE;
                    newState = 2;
                    shots = 0;
                    wasHit = false;
                } else if ((ang>20&&ang<180)|| (ang<340&&ang>180)) {
                    log("Ang: "+sensors.angularVelocity);
                    actions.leftTrackCoef = -0.5;
                    actions.rightTrackCoef = 0.5;
                    if (sensors.angularVelocity > 180) {
                        actions.leftTrackCoef *= -1;
                        actions.rightTrackCoef *= -1;
                    }
                }
                else if (Math.abs(sensors.velocity.z) > 40) {
                    log("Vel: "+ sensors.velocity.z);
                    actions.leftTrackCoef = actions.rightTrackCoef = -0.5
                    if (sensors.velocity.z < 0) {
                        actions.leftTrackCoef *= -1;
                        actions.rightTrackCoef *= -1;
                    }
                }
                else {
                    newState = 0;
                    log(sensors.angularVelocity);
                }

                break;
            default:
                break;
        }
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