function setup(){
	var rec = function(a){
		log(a);
		rec(a+1);
	}
	rec(1);
}