//called once when robot is spawned on field
var logPeriod = 0.5;
var nextTimeToLog = 0;
function setup(){
	log("setting up");
 logPeriod = 0.5;
nextTimeToLog = 0;
}


function loop(){
	if(sensors.time > nextTimeToLog)
{
nextTimeToLog = sensors.time+logPeriod;
	var s = "";
Object.keys(sensors.proxor).forEach(function(key,index) {
    // key: the name of the object key
    // index: the ordinal position of the key within the object 
	s+=key+"\n";
});
	log(s);
}
}