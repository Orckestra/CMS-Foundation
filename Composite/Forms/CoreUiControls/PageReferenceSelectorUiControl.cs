﻿using System.ComponentModel;

using Composite.Data;
using Composite.Forms.Foundation;
using System;
using Composite.Data.Types;


namespace Composite.Forms.CoreUiControls
{
    [DefaultBindingProperty("Selected")]
    public class PageReferenceSelectorUiControl : UiControl
    {
        private DataReference<IPage> _selected = null;

        [BindableProperty()]
        [FormsProperty()]
        public DataReference<IPage> Selected 
        { 
            get
            {
                return _selected;
            }
            set
            {
                _selected = value;
            }
        }
    }
}
