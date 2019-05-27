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
		log("Taking fire from " + relativeAzimuthToSource + "; \nAmmount: "+ammount);
}

/**
 * @param {Number} ammount
 * @param {Number} relativeAzimuth
 * @param {String} category
 */
function onProximityEnter(distance, relativeAzimuth, category){
			log("In proximity with "+category+"\n"+relativeAzimuth);
}

/**
 * @param {Number} ammount
 * @param {Number} relativeAzimuth
 * @param {String} category
 */
function onCollisionEnter(distance, relativeAzimuth, category){
		log("Collision with "+category+"\n"+relativeAzimuth);
}