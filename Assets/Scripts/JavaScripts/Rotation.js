
var nextTimeToLog = 0;

//called once when robot is spawned on field
function setup(){
	actions.leftTrackCoef = 0.22;
	actions.rightTrackCoef = 0.2;
	actions.turretAngularCoef = 1;
	actions.radarAngularCoef = -1/3;
	
	nextTimeToLog = 0;
}

//called repeatedly every cycle of physics update (approximatly every 0.02 seconds)
function loop(){
	if(sensors.time>=nextTimeToLog)
	{
		log(sensors.azimuth);
		nextTimeToLog = sensors.time + 1; //one second
	}
}