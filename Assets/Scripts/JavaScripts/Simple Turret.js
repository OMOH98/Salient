//called once when robot is spawned on field
var	nextTimeToLog = 0;
function setup(){
	log("setting up!");
	actions.leftTrackCoef = 0;
	actions.rightTrackCoef = 0;
	nextTimeToLog = 0;
}

//called repeatedly every cycle of physics update (approximatly every 0.02 seconds)
function loop(){
	if(sensors.time>nextTimeToLog){
		log(sensors.radar.category);
		nextTimeToLog = sensors.time + 1;//1 second
	}
	if(sensors.radar.distance < Number.POSITIVE_INFINITY){
		if(sensors.radar.category == "Enemy")
		{
			actions.turretAngularCoef = 0;
			actions.radarAngularCoef = 0;
			actions.fireShots = 1;
		}
		else{
			actions.turretAngularCoef = 1;
			actions.radarAngularCoef = 1/3;
		}
	}
	else
	{
		actions.turretAngularCoef = 1;
		actions.radarAngularCoef = 1/3;
	}
}