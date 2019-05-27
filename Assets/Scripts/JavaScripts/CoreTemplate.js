/**
 * 
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
