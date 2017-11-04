/// <reference path="../lib/RequireJS/require.debug.js" />
/// <reference path="presenter.js" />


(function () {


    //config
    requirejs.config({
        baseUrl: '/Scripts/app',
        paths:
            {
                'mods' : '/Scripts/mods'
            }
    });

   
    var root = this;

    defineLibs();
    definePlugins();

    function defineLibs() {
        define('jquery', [], function () { return root.jQuery; });
        define('ko', [], function () { return root.ko; });
        define('sammy', [], function () { return root.Sammy; });
    }

    function definePlugins() {
        //todo: plugins
        requirejs([
            'ko.bindings'
        ],
            boot);
    };

    function boot() {
        require(['application'], function (app) { app.start() });
    }
})();