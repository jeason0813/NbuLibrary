/// <reference path="../lib/RequireJS/require.debug.js" />
/// <reference path="../lib/jQuery/jquery-1.9.1.js" />
/// <reference path="../lib/Sammy/sammy-0.7.4.js" />


define('application',
    ['jquery', 'ko', 'router', 'profile', 'plugins', 'viewmgr', 'uitexts', 'templates', 'reports'],
    function (jQuery, ko, router, nav, tp, users) {

        function start() {

            router.start();

        };

        //interface
        return {
            start: start
        };
    });