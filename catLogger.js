//called once when robot is spawned on field
var logPeriod = 0.5;
var nextTimeToLog = 0;
function setup(){
	log("setting up");
	actions.leftTrackCoef = 0.1;
	actions.rightTrackCoef = 0.12;
 logPeriod = 0.5;
nextTimeToLog = 0;
}


function loop(){
	if(sensors.time > nextTimeToLog)
{
nextTimeToLog = sensors.time+logPeriod;
log(sensors.radar.category);
}
}