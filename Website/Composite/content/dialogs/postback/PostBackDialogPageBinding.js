PostBackDialogPageBinding.prototype = new DialogPageBinding;
PostBackDialogPageBinding.prototype.constructor = PostBackDialogPageBinding;
PostBackDialogPageBinding.superclass = DialogPageBinding.prototype;

/**
 * @class
 */
function PostBackDialogPageBinding () {

	/**
	 * @type {SystemLogger}
	 */
	this.logger = SystemLogger.getLogger ( "PostBackDialogPageBinding" );

	/**
	 * @type {string}
	 */
	this._url = null;
	
	/**
	 * @type {List<object>}
	 */
	this._list = null;
	
	/*
	 * Returnable.
	 */
	return this;
}

/**
 * Identifies binding.
 */
PostBackDialogPageBinding.prototype.toString = function () {

	return "[PostBackDialogPageBinding]";
}

/**
 * @param {object} arg
 */
PostBackDialogPageBinding.prototype.setPageArgument = function ( arg ) {
	
	PostBackDialogPageBinding.superclass.setPageArgument.call ( this, arg );
	
	this._url = arg.url;
	this._list = arg.list;
}

/**
 * Submit on ready. Investigations should be instigated  
 * to further study how we can make this go a lot faster.
 * @overloads {DialogPageBinding#onAfterPageInitialize}
 */
PostBackDialogPageBinding.prototype.onAfterPageInitialize = function () {
	
	PostBackDialogPageBinding.superclass.onAfterPageInitialize.call ( this );
	this._submit ();
}

/**
 * Submit the data to the specified URL.
 */
PostBackDialogPageBinding.prototype._submit = function () {
	
	var form = this.bindingDocument.forms [ 0 ];
	form.action = top.Resolver.resolve ( this._url );
	
	var isDebugging = true;
	var debug = "Posting to: " + form.action +"\n\n";
	
	this._list.reset ();
	while ( this._list.hasNext ()) {
	
		var entry = this._list.getNext ();
		var input = this.bindingDocument.createElement ( "input" );
		input.name = entry.name;
		input.value =  entry.value;
		input.type = "hidden";
		form.appendChild ( input );
		
		if ( isDebugging ) {
			debug += entry.name + ": " + entry.value  + "\n";
		}
	}
	if ( isDebugging ) {
		this.logger.debug ( debug );
	}
	form.submit ();
}