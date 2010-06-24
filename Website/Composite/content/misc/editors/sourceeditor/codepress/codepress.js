/*
 * CodePress - Real Time Syntax Highlighting Editor written in JavaScript - http://codepress.org/
 * 
 * Copyright (C) 2006 Fernando M.A.d.S. <fermads@gmail.com>
 *
 * This program is free software; you can redistribute it and/or modify it under the terms of the 
 * GNU Lesser General Public License as published by the Free Software Foundation.
 * 
 * Read the full licence: http://www.opensource.org/licenses/lgpl-license.php
 */

CodePress = function(obj) {
	
	var self = document.createElement('iframe');
	self.textarea = obj;
	self.textarea.disabled = true;
	self.textarea.style.overflow = 'hidden';
	self.style.height = "100%";
	self.style.width = "100%";
	self.textarea.style.overflow = 'auto';
	self.frameBorder = 0; // remove IE internal iframe border
	self.style.visibility = 'hidden';
	self.style.position = 'absolute';
	self.options = self.textarea.className;
	
	self.initialize = function() {
		self.editor = self.contentWindow.CodePress;
		self.editor.body = self.contentWindow.document.getElementsByTagName('body')[0];
		self.editor.setCode(self.textarea.value);
		self.setOptions();
		self.editor.syntaxHighlight('init');
		self.textarea.style.display = 'none';
		self.style.position = 'static';
		self.style.visibility = 'visible';
		self.style.display = 'inline';
		CodePress.runComposite ( self ); // ADDED BY COMPOSITE!
	}
	
	// obj can by a textarea id or a string (code)
	self.edit = function(obj,language) {
		if(obj) self.textarea.value = document.getElementById(obj) ? document.getElementById(obj).value : obj;
		if(!self.textarea.disabled) return;
		self.language = language ? language : self.getLanguage();
		self.src = CodePress.path+'codepress.html?language='+self.language+'&ts='+(new Date).getTime();
		if(self.attachEvent) self.attachEvent('onload',self.initialize);
		else self.addEventListener('load',self.initialize,false);
	}

	self.getLanguage = function() {
		for (language in CodePress.languages) 
			if(self.options.match('\\b'+language+'\\b')) 
				return CodePress.languages[language] ? language : 'generic';
	}
	
	self.setOptions = function() {
		if(self.options.match('autocomplete-off')) self.toggleAutoComplete();
		if(self.options.match('readonly-on')) self.toggleReadOnly();
		if(self.options.match('linenumbers-off')) self.toggleLineNumbers();
	}
	
	self.getCode = function() {
		return self.textarea.disabled ? self.editor.getCode() : self.textarea.value;
	}

	self.setCode = function(code) {
		self.textarea.disabled ? self.editor.setCode(code) : self.textarea.value = code;
		self.editor.syntaxHighlight('init'); // added by composite
	}

	self.toggleAutoComplete = function() {
		self.editor.autocomplete = (self.editor.autocomplete) ? false : true;
	}
	
	self.toggleReadOnly = function() {
		self.textarea.readOnly = (self.textarea.readOnly) ? false : true;
		if(self.style.display != 'none') // prevent exception on FF + iframe with display:none
			self.editor.readOnly(self.textarea.readOnly ? true : false);
	}
	
	self.toggleLineNumbers = function() {
		var cn = self.editor.body.className;
		self.editor.body.className = (cn==''||cn=='show-line-numbers') ? 'hide-line-numbers' : 'show-line-numbers';
	}
	
	self.toggleEditor = function() {
		if(self.textarea.disabled) {
			self.textarea.value = self.getCode();
			self.textarea.disabled = false;
			self.style.display = 'none';
			self.textarea.style.display = 'inline';
		}
		else {
			self.textarea.disabled = true;
			self.setCode(self.textarea.value);
			self.editor.syntaxHighlight('init');
			self.style.display = 'inline';
			self.textarea.style.display = 'none';
		}
	}

	self.edit();
	return self;
}

CodePress.languages = {	
	csharp : 'C#', 
	css : 'CSS', 
	generic : 'Generic',
	html : 'HTML',
	java : 'Java', 
	javascript : 'JavaScript', 
	perl : 'Perl', 
	ruby : 'Ruby',	
	php : 'PHP', 
	text : 'Text', 
	sql : 'SQL',
	vbscript : 'VBScript',
	xsl : 'XSL',
	xml : 'XML' // ADDED BY COMPOSITE!
}


CodePress.run = function() {
	s = document.getElementsByTagName('script');
	for(var i=0,n=s.length;i<n;i++) {
		if(s[i].src.match('codepress.js')) {
			CodePress.path = s[i].src.replace('codepress.js','');
		}
	}
	t = document.getElementsByTagName('textarea');
	
	for(var i=0,n=t.length;i<n;i++) {
		if(t[i].className.match('codepress')) {
			id = t[i].id;
			t[i].id = id+'_cp';
			eval(id+' = new CodePress(t[i])');
			t[i].parentNode.insertBefore(eval(id), t[i]);
		} 
	}
}

/*
 * Composite added this stuff.
 * @param {HTMLIframeElement} codepressframe
 */
CodePress.runComposite = function ( codepressframe ) {

	if ( top.Application != null ) {
		
		/*
		 * Initialize Composite extensions.
		 */
		if ( top.Client.isMozilla == true ) {
			
			var tabmanagerimpl = codepressframe.contentWindow.CodePressTabManager;
			if ( tabmanagerimpl != null ) {
				new tabmanagerimpl ().manage ( codepressframe.contentWindow );
			}
			
			var undomanagerimpl = codepressframe.contentWindow.CodePressUndoManager;
			if ( undomanagerimpl != null ) {
				new undomanagerimpl ().manage ( codepressframe.contentWindow );
			}
			
			var copypasteimpl = codepressframe.contentWindow.CodePressCopyPasteManager; 
			if ( copypasteimpl != null ) {
				new copypasteimpl ().manage ( codepressframe.contentWindow );
			}
		}
		
		/*
		 * Broadcast intercepted by SourceCodeEditorBinding.
		 */
		top.EventBroadcaster.broadcast (
			top.BroadcastMessages.CODEPRESS_INITIALIZED, {
				broadcastWindow	: window,
				codePressFrame : codepressframe
			}
		);
	}
}

if(window.attachEvent) window.attachEvent('onload',CodePress.run);
else window.addEventListener('DOMContentLoaded',CodePress.run,false);