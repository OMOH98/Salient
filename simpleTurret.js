//called once when robot is spawned on field
var nt = 0;
function setup(){
	log("setting up!");
	actions.leftTrackCoef = 0;
	actions.rightTrackCoef = 0;
	nt = 0;
}

//called repeatedly every cycle of physics update (approximatly every 0.02 seconds)
function loop(){
if(sensors.time>nt){
	log(sensors.radar.category);
	nt = sensors.time + 1;
}
	if(sensors.radar.distance < Number.POSITIVE_INFINITY){
		if(sensors.radar.category == 2)
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