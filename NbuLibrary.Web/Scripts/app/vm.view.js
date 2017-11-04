define('vm.view',
	['ko', 'viewservice', 'presenter', 'entityservice', 'textprovider', 'entitymapper', 'messanger'],
	function (ko, vs, presenter, es, tp, em, msgr) {

	    //enums
	    var RelTypes = {
	        OneToOne: 0,
	        OneToMany: 1,
	        ManyToOne: 2,
	        ManyToMany: 3
	    };

	    var ViewTypes = {
	        Textfield: 0,
	        Numberfield: 1,
	        Datefield: 2,
	        Enumfield: 3,
	        Checkfield: 4,
	        Listfield: 5,
	        Filefield: 6,
            Htmlfield: 7
	    };

	    var EditTypes = {
	        Textbox: 0,
	        Numberbox: 1,
	        Datepicker: 2,
	        Enumlist: 3,
	        Selectlist: 4,
	        Checkbox: 5,
	        FileUpload: 6,
	        Autocomplete: 7,
            Htmlbox: 8
	    };

	    //String,0
	    //Number,1
	    //Boolean,2
	    //Enum,3
	    //Sequence,4
	    //Computed,5
	    //Datetime 6
	    var PropertyTypes = {
	        String: 0,
	        Number: 1,
	        Boolean: 2,
	        EnumValue: 3,
	        Sequence: 4,
	        Computed: 5,
	        Date: 6

	    };
	    //-------------------

	    var UITypes = {
	        'Grid': 0,
	        'Form': 1,
	        'View': 2
	    };

	    var loaded = ko.observable(false);

	    var _name = ko.observable();
	    var _label = ko.observable();
	    var _entity = ko.observable();
	    var _fields = ko.observableArray([]);
	    var _type = ko.observable();

	    function vmNewField(entity) {

	        var unused = [];
	        ko.utils.arrayForEach(entity.Properties, function (prop) {
	            if (!ko.utils.arrayFirst(_fields(), function (x) { return x.Property == prop.Name }))
	                unused.push(prop);
	        });

	        var _properties = ko.observableArray(unused);
	        var _selected = ko.observable();
	        var _type = ko.observable();
	        var _types = ko.computed(function () {
	            if (!_selected())
	                return [];

	            //console.log('types', _selected());
	            if (_selected().Name === "Body" && entity.Name === 'Notification')
	                return [{ Type: ViewTypes.Htmlfield, Name: 'Htmlfield' },
	                { Type: ViewTypes.Textfield, Name: 'Textfield' }];
	            switch (_selected().Type) {
	                case PropertyTypes.String:
	                case PropertyTypes.Sequence:
	                case PropertyTypes.Computed:
	                    return [{ Type: ViewTypes.Textfield, Name: 'Textfield' }];
	                case PropertyTypes.Date:
	                    return [{ Type: ViewTypes.Datefield, Name: 'Datefield' }];
	                case PropertyTypes.Number:
	                    return [{ Type: ViewTypes.Numberfield, Name: 'Numberfield' }];
	                case PropertyTypes.Boolean:
	                    return [{ Type: ViewTypes.Checkfield, Name: 'Checkfield' }];
	                case PropertyTypes.EnumValue:
	                    return [{ Type: ViewTypes.Enumfield, Name: 'Enumfield' }];
	                default:
	                    //TODO: Dev only code!
	                    throw new Error("Unknown property type.");
	            }
	        });

	        function createField() {
	            var o = {
	                Property: _selected().Name,
	                Type: _type().Type,
	                Label: _selected().Name,
	                Role: null,
	                Order: _fields().length + 1,

	                //TODO: hardcoded!
	                Length: _selected().Length,
	                Multiline: false
	            };

	            if (_selected().Type == PropertyTypes.EnumValue)
	                o['EnumClass'] = _selected().EnumType;

	            return o;
	        }

	        return {
	            template: 'UIDefinition_NewViewField',
	            Properties: _properties,
	            Selected: _selected,
	            SelectedType: _type,
	            ViewTypes: _types,
	            createViewField: createField
	        };
	    };

	    function vmNewRel(entity) {

	        var _selectedRel = ko.observable();
	        var _selectedProp = ko.observable();
	        var _selectedType = ko.observable();

	        var _relations = [];

	        //all relations of the entity are presented to the user
	        ko.utils.arrayForEach(entity.Relations, function (rel) {
	            var entityName = rel.LeftEntity == entity.Name ? rel.RightEntity : rel.LeftEntity

	            //var existing = ko.utils.arrayFirst(_fields(), function (x) { return x.Role == rel.Role && x.Entity == entityName; });
	            //if (!existing) {
	            var relType = em.getRelationType(rel, entity);
	            _relations.push({
	                Entity: entityName,
	                Role: rel.Role,
	                Type: relType,
	                Multiple: relType == RelTypes.ManyToMany || relType == RelTypes.OneToMany,
	                Name: [rel.Role, ' (', entityName, ')'].join(""),
	                Properties : rel.Properties
	            });
	            //}
	        });


	        var _properties = ko.observableArray();

	        //populate unused properties
	        _selectedRel.subscribe(function () {
	            if (!_selectedRel())
	                return [];

	            _properties.removeAll();
	            $.when(es.getEntity(_selectedRel().Entity))
                .done(function (relEntity) {
                    //only properties that are not used are presented as options to the user
                    ko.utils.arrayForEach(relEntity.Properties, function (prop) {
                        if (!ko.utils.arrayFirst(_fields(), function (f) { return f.Entity == _selectedRel().Entity && f.Role == _selectedRel().Role && f.Property == prop.Name })) {
                            prop.IsRelation = false;
                            _properties.push(prop);
                        }
                    });                    

                    ko.utils.arrayForEach(_selectedRel().Properties, function (prop) {
                        if (!ko.utils.arrayFirst(_fields(), function (f) { return f.Entity == _selectedRel().Entity && f.Role == _selectedRel().Role && f.Property == prop.Name })
                           && prop.Name.toLowerCase() != 'id' && prop.Name.toLowerCase() != 'lid' && prop.Name.toLowerCase() != 'rid') {
                            prop.IsRelation = true;
                            _properties.push(prop);
                        }
                    });
                });
	        });

	        var _types = ko.computed(function () {
	            if (!_selectedProp())
	                return [];

	            //console.log('types', _selectedProp());

	            var res = [];
	            if (_selectedRel().Entity.toLowerCase() == 'file')
	                res.push({ Type: ViewTypes.Filefield, Name: 'Filefield' })

	            if (!_selectedRel().Multiple) {
	                switch (_selectedProp().Type) {
	                    case PropertyTypes.String:
	                    case PropertyTypes.Computed:
	                        res.push({ Type: ViewTypes.Textfield, Name: 'Textfield' });
	                        break;
	                    case PropertyTypes.Date:
	                        res.push({ Type: ViewTypes.Datefield, Name: 'Datefield' });
	                        break;
	                    case PropertyTypes.Number:
	                        res.push({ Type: ViewTypes.Numberfield, Name: 'Numberfield' });
	                        break;
	                    case PropertyTypes.Boolean:
	                        res.push({ Type: ViewTypes.Checkfield, Name: 'Checkfield' });
	                        break;
	                    case PropertyTypes.EnumValue:
	                        res.push({ Type: ViewTypes.Enumfield, Name: 'Enumfield' });
	                        break;
	                    default:
	                        //TODO: Dev only code!
	                        throw new Error("Unknown property type.");
	                }
	            }
	            else {
	                res.push({ Type: ViewTypes.Listfield, Name: 'Listfield' });
	            }
	            return res;
	        });

	        function createField() {
	            var o = {
	                Property: _selectedProp().Name,
	                Type: _selectedType().Type,
	                Label: _selectedProp().Name,
	                Role: _selectedRel().Role,
	                Entity: _selectedRel().Entity,
	                Order: _fields().length + 1,
	                //TODO: hardcoded!
	                Length: _selectedProp().Length,
	                Multiline: false
	            };

	            if (_selectedProp().Type == PropertyTypes.EnumValue)
	                o['EnumClass'] = _selectedProp().EnumType;
	            else if (_selectedType().Type == ViewTypes.Filefield)
	                o.Multiple = _selectedRel().Multiple;
	            return o;
	        }

	        return {
	            template: 'UIDefinition_NewViewRelation',

	            Relations: _relations,
	            Properties: _properties,
	            ViewTypes: _types,
	            SelectedRelation: _selectedRel,
	            SelectedProperty: _selectedProp,
	            SelectedType: _selectedType,

	            createViewField: createField
	        };
	    };

	    function vmNewEdit(entity) {
	        var unused = [];
	        ko.utils.arrayForEach(entity.Properties, function (prop) {
	            if (!ko.utils.arrayFirst(_fields(), function (x) { return x.Property == prop.Name }))
	                unused.push(prop);
	        });

	        var _properties = ko.observableArray(unused);
	        var _selected = ko.observable();
	        var _type = ko.observable();
	        var _types = ko.computed(function () {
	            if (!_selected())
	                return [];

	            //console.log('types', _selected());

	            if (_selected().Name === "Body" && entity.Name === 'Notification')
	                return [{ Type: EditTypes.Htmlbox, Name: 'Htmlbox' },
                        { Type: EditTypes.Textbox, Name: 'Textbox' }];

	            switch (_selected().Type) {
	                case PropertyTypes.String:
	                    //TODO:computed!
	                    //case PropertyTypes.Computed:
	                    return [{ Type: EditTypes.Textbox, Name: 'Textbox' }];
	                case PropertyTypes.Date:
	                    return [{ Type: EditTypes.Datepicker, Name: 'Datepicker' }];
	                case PropertyTypes.Number:
	                    return [{ Type: EditTypes.Numberbox, Name: 'Numberbox' }];
	                case PropertyTypes.Boolean:
	                    return [{ Type: EditTypes.Checkbox, Name: 'Checkfield' }];
	                case PropertyTypes.EnumValue:
	                    return [{ Type: EditTypes.Enumlist, Name: 'Enumfield' }];
	                default:
	                    //TODO: Dev only code!
	                    throw new Error("Unknown property type.");
	            }
	        });

	        function createField() {
	            var o = {
	                Property: _selected().Name,
	                Type: _type().Type,
	                Label: _selected().Name,
	                Order: _fields().length + 1,
	                Required: false
	            };

	            switch (_type().Type) {
	                case EditTypes.Textbox:
	                    o.MinLength = 0;
	                    o.MaxLength = _selected().Length;
	                    o.Multiline = false;
	                    break;
	                case EditTypes.Enumlist:
	                    o['EnumClass'] = _selected().EnumType;
	                    break;
	                case EditTypes.Datepicker:
	                    o.Min = null;
	                    o.Max = null;
	                    o.DaysOffset = null;
	                    o.Future = null;
	                    break;
	                case EditTypes.Numberbox:
	                    o.Min = null;
	                    o.Max = null;
	                    o.Integer = _selected().IsInteger;
	                    break;
	                case EditTypes.Checkbox:
	                    break;
	            }

	            return o;
	        }

	        return {
	            template: 'UIDefinition_NewViewField',
	            Properties: _properties,
	            Selected: _selected,
	            SelectedType: _type,
	            ViewTypes: _types,
	            createViewField: createField
	        };
	    };

	    function vmNewEditRel(entity) {

	        var _selectedRel = ko.observable();
	        var _selectedProp = ko.observable();
	        var _selectedType = ko.observable();

	        var _relations = [];

	        //all relations of the entity are presented to the user
	        ko.utils.arrayForEach(entity.Relations, function (rel) {
	            var entityName = rel.LeftEntity == entity.Name ? rel.RightEntity : rel.LeftEntity

	            var existing = ko.utils.arrayFirst(_fields(), function (x) { return x.Role == rel.Role && x.Entity == entityName; });
	            if (!existing) {
	                _relations.push({
	                    Entity: entityName,
	                    Role: rel.Role,
	                    Type: em.getRelationType(rel, entity),
	                    Name: [rel.Role, ' (', entityName, ')'].join("")
	                });
	            }
	        });


	        var _properties = ko.observableArray();

	        _selectedRel.subscribe(function () {
	            if (!_selectedRel())
	                return [];

	            _properties.removeAll();
	            $.when(es.getEntity(_selectedRel().Entity))
                .done(function (relEntity) {
                    ko.utils.arrayForEach(relEntity.Properties, function (prop) {
                        _properties.push(prop);
                    });
                });
	        });

	        var _types = ko.computed(function () {
	            if (!_selectedProp())
	                return [];
	            else if (_selectedRel().Entity.toLowerCase() == 'file')
	                return [{ Type: EditTypes.FileUpload, Name: 'FileUpload' }, { Type: EditTypes.Selectlist, Name: 'Selectlist' }, { Type: EditTypes.Autocomplete, Name: 'Autocomplete' }];
	            else
	                return [{ Type: EditTypes.Selectlist, Name: 'Selectlist' }, { Type: EditTypes.Autocomplete, Name: 'Autocomplete' }];
	        });

	        function createField() {
	            var o = {
	                Property: _selectedProp().Name,
	                Type: _selectedType().Type,
	                Label: _selectedRel().Name,
	                Role: _selectedRel().Role,
	                Entity: _selectedRel().Entity,
	                Multiple: _selectedRel().Type == RelTypes.ManyToMany || _selectedRel().Type == RelTypes.OneToMany,
	                Order: _fields().length + 1,
	                Required: false
	            };
	            if (o.Type == EditTypes.Selectlist) {
	                o.Formula = ['{', _selectedProp().Name, '}'].join('');
	            }

	            return o;
	        }

	        return {
	            template: 'UIDefinition_NewViewRelation',

	            Relations: _relations,
	            Properties: _properties,
	            ViewTypes: _types,
	            SelectedRelation: _selectedRel,
	            SelectedProperty: _selectedProp,
	            SelectedType: _selectedType,

	            createViewField: createField
	        };
	    };

	    function bind(name) {
	        loaded(false);
	        _fields.removeAll();
	        $.when(vs.get(name))
            .done(function (view) {
                ko.utils.arrayForEach(view.Fields, function (field) {
                    _fields.push(field);
                });
                _name(view.Name);
                _label(view.Label);
                _type(view.Type);
                _entity(view.Entity);
                loaded(true);
            });
	    };

	    function templateForViewField(viewField) {
	        switch (viewField.Type) {
	            case ViewTypes.Textfield:
	                return 'UIDefinition_Textfield';
	            case ViewTypes.Numberfield:
	            case ViewTypes.Datefield:
	            case ViewTypes.Checkfield:
	            case ViewTypes.Enumfield:
	            case ViewTypes.Filefield:
	            case ViewTypes.Listfield:
	            case ViewTypes.Htmlfield:
	                return 'UIDefinition_Commonfield';
	            default:
	                throw new Error("Unknown ViewField type:" + viewField.Type);
	        }
	    };

	    function templateForEditField(editField) {
	        switch (editField.Type) {
	            case EditTypes.Textbox:
	                return 'UIDefinition_Textbox';
	            case EditTypes.Numberbox:
	                return 'UIDefinition_Numberbox';
	            case EditTypes.Datepicker:
	                return 'UIDefinition_Datepicker';
	            case EditTypes.Checkbox:
	            case EditTypes.Enumlist:
	            case EditTypes.FileUpload:
	            case EditTypes.Autocomplete:
	            case EditTypes.Htmlbox:
	                return 'UIDefinition_Commoneditor';
	            case EditTypes.Selectlist:
	                return 'UIDefinition_Selectlist';
	            default:
	                throw new Error("Unknown EditField type:" + editField.Type);

	        }
	    };

	    function templateFor(field) {
	        switch (_type()) {
	            case UITypes.Grid:
	            case UITypes.View:
	                return templateForViewField(field);
	            case UITypes.Form:
	                return templateForEditField(field);
	        }
	    };

	    function addField() {
	        $.when(es.getEntity(_entity()))
            .done(function (entity) {


                var fld = null;

                if (_type() == UITypes.Form)
                    fld = vmNewEdit(entity);
                else
                    fld = vmNewField(entity);

                var popup = presenter.popup(fld);
                popup.ok(tp.get('btn_add'), function () {
                    _fields.push(fld.createViewField());
                    popup.close();
                });
                popup.show();
            });
	    };

	    function addRel() {
	        $.when(es.getEntity(_entity()))
            .done(function (entity) {

                var fld = _type() == UITypes.Form ? vmNewEditRel(entity) : vmNewRel(entity);

                var popup = presenter.popup(fld);
                popup.ok(tp.get('btn_add'), function () {
                    _fields.push(fld.createViewField());
                    popup.close();
                });
                popup.show();
            });
	    };

	    function deleteField(field) {
	        _fields.remove(field);
	    }

	    function save() {
	        var view = {
	            Name: _name(),
	            Label: _label(),
	            Entity: _entity(),
	            Type: _type(),
	            Fields: _fields()
	            //Filters: viewDef.Filters ? viewDef.Filters() : null
	        };

	        $.when(vs.save(view)).done(function () {
	            msgr.success(tp.get('msg_saved_ok'));
	        });
	    };

	    return {
	        template: 'UIDefinition',
	        Loaded: loaded,
	        bind: bind,
	        add: addField,
	        addRelation: addRel,
	        save: save,
	        remove: deleteField,

	        templateFor: templateFor,
	        Name: _name,
	        Fields: _fields
	    };
	});