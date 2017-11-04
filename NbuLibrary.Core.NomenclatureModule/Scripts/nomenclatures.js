define('mods/nomenclatures',
    ['jquery', 'ko', 'dataservice', 'usercontext', 'navigation', 'router', 'presenter', 'textprovider', 'grid', 'entitypage', 'datacontext', 'messanger'],
    function ($, ko, ds, usercontext, nav, router, presenter, tp, entitygrid, EP, datacontext, msgr) {

        var Permissions = {
            Manage: 'Manage'
        };
        MODULE_ID = 2;

        var grid = new entitygrid.Grid();

        if (usercontext.hasPermission(MODULE_ID, Permissions.Manage)) {
            router.add('#/nomenclatures/', function () {

                $.get('/api/Nomenclature/GetNomenclatures', { ts: new Date().getTime() }, function (noms) {

                    var vm = {
                        items: ko.observableArray(noms),
                        template: 'Nomenclatures_Grid'
                    };

                    presenter.show(vm);

                });

            });
            router.add('#/nomenclature/:name', function () {
                var nom = this.name;
                var query = ds.search(nom).allProperties(true);
                query.sort('DisplayOrder');
                grid.bind(query, {
                    Fields: [
                        { Property: 'Value', Label: tp.get('lbl_nomenclatures_value'), Type: 0 },
                        { Property: 'DisplayOrder', Label: tp.get('lbl_nomenclatures_displayorder'), Type: 1 }
                    ]
                });
                var vm = (function () {

                    function create() {
                        var page = new EP.Page();
                        page.bind(nom, {
                            Fields: [
                                { Property: 'Value', Label: tp.get('lbl_nomenclatures_value'), Type: 0, Required: true },
                                { Property: 'DisplayOrder', Label: tp.get('lbl_nomenclatures_displayorder'), Type: 1, Required: true }
                            ]
                        });
                        page.template = 'GeneralForm';
                        $.when(page.loadDefaults()).done(function () {
                            datacontext.bind(nom, page.Entity);
                        });


                        var popup = presenter.popup(page, {title : tp.get('heading_nomenclature_new')});
                        popup.ok(tp.get('btn_save'), function () {
                            if (datacontext.hasChanges()) {
                                $.when(datacontext.save())
                                .done(function (res) {
                                    datacontext.clear();
                                    msgr.success(tp.get('msg_saved_ok'));
                                    popup.close();
                                    grid.reload();
                                });
                            }
                        });
                        popup.show();
                    };
                    function edit(item) {
                        var page = new EP.Page();
                        page.bind(nom, {
                            Fields: [
                                { Property: 'Value', Label: tp.get('lbl_nomenclatures_value'), Type: 0, Required: true },
                                { Property: 'DisplayOrder', Label: tp.get('lbl_nomenclatures_displayorder'), Type: 1, Required: true }
                            ]
                        }, item.Id());
                        page.template = 'GeneralForm';
                        $.when(page.load()).done(function () {
                            datacontext.bind(nom, page.Entity, item.Id());
                        });


                        var popup = presenter.popup(page, {title : tp.get('heading_nomenclature_edit')});
                        popup.ok(tp.get('btn_save'), function () {
                            if (datacontext.hasChanges()) {
                                $.when(datacontext.save())
                                .done(function (res) {
                                    datacontext.clear();
                                    msgr.success(tp.get('msg_saved_ok'));
                                    popup.close();
                                    grid.reload();
                                });
                            }
                        });
                        popup.show();
                    }
                    function remove(item) {
                        var ask = presenter.ask(tp.get('confirm_entity_deletion'));
                        ask.yes(function () {
                            $.when(ds.remove(nom, item.Id(), false))
                            .done(function () {
                                grid.Items.remove(item);
                            });
                        });
                        ask.show();
                    }


                    return {
                        Nomenclature: nom,
                        template: 'Nomenclatures_Manage',
                        Items: grid.Items,
                        Definition: grid.Definition,
                        LoadedDef: grid.LoadedDef,
                        Loaded: grid.Loaded,
                        Sorting: grid.Sorting,
                        Paging: grid.Paging,
                        //remove: remove,
                        sort: grid.sort,
                        create: create,
                        edit: edit,
                        remove: remove
                    };
                })();

                presenter.show(vm)
            });

            nav.add(tp.get('nav_nomenclatures'), '#/nomenclatures/');
        }
    });