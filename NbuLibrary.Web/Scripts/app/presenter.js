/// <reference path="../lib/RequireJS/require.debug.js" />

define('presenter',
    ['ko', 'jquery', 'textprovider'],
    function (ko, $, tp) {

        function show(vm) {
            var main = $('article')[0];
            ko.cleanNode(main);
            ko.applyBindings(vm, main);
        };

        function prepareViewport() {
            var main = $('article')[0];
            ko.cleanNode(main);
            return main;
        };

        function ask(question) {
            var modal = $('#modalWindow').clone().appendTo(document.body);

            modal.find('#modalTitle').html(tp.get('heading_ask'));
            var modalBody = modal.find('.modal-body');
            modalBody.html(question);
            //modalBody.attr('data-bind', "template : {name : template}");
            //ko.applyBindings(vm, modalBody[0]);

            modal.bind('hidden', function () {
                //ko.cleanNode(modal.find('.modal-body')[0]);
                modal.remove();
            });

            modal.find('.modal-footer .cancel').text(tp.get('btn_no'))

            function show() {
                modal.modal('show');
            };

            function close() {
                modal.modal('hide');
            }

            function yes(callback) {
                modal.find('.modal-footer .save').text(tp.get('btn_yes')).bind('click', function () {
                    callback();
                    close();
                });
            };

            return {
                show: show,
                close: close,
                yes: yes
            };
        };

        function popup(vm, opts) {
            var modal = $('#modalWindow').clone().appendTo(document.body);
            var opts = opts || {};

            opts = $.extend({ fullWidth: false, title: '&nbsp;' }, opts);

            if (opts.fullWidth)
                modal.addClass('container');

            modal.find('#modalTitle').html(opts.title);
            modal.find('.modal-footer .cancel').text(tp.get('btn_close')).prop('disabled', false);
            modal.find('.modal-footer .save').hide().prop('disabled', false);

            var modalBody = modal.find('.modal-body');
            modalBody.attr('data-bind', "template : {name : ko.isObservable(template) ?  template() : template}");
            ko.applyBindings(vm, modalBody[0]);

            ////if (opt.act) {
            //    modal.find('.modal-footer .save')
            //        .show()
            //        .text(opt.act.title)
            //        .bind('click', function (e) {
            //            e.preventDefault();

            //            var form = modalBody.find('form');
            //            if (form.length > 0 && !opt.noValidate) {
            //                if (!form.valid())
            //                    return;
            //            }
            //            opt.act.callback(vm);
            //        });
            //}
            //else
            //    modal.find('.modal-footer .save').hide();

            modal.bind('hidden', function () {
                if (opts.onClose)
                    opts.onClose();

                ko.cleanNode(modal.find('.modal-body')[0]);
                modal.remove();
            });

            function show() {
                modal.modal('show');
            };

            function close() {
                modal.modal('hide');
            }

            function ok(lbl, callback) {
                modal.find('.modal-footer .save').text(lbl).show().bind('click', function (e) {
                    var form = modalBody.find('form');
                    if (form.length) {
                        if (form.valid())
                            callback.apply(this, e);
                    }
                    else
                        callback.apply(this, e);

                });
            };


            return {
                show: show,
                close: close,
                ok: ok,
                getForm: function () { return modalBody.find('form'); }
            };
        };

        return {
            show: show,
            prepareViewport:prepareViewport,
            popup: popup,
            ask: ask,
            getMainForm: function () { var form = $('article form'); if (form.length) return form; else return { valid: function () { return true; } } }//TODO: dirty fix for getMainForm().valid() used in several ViewModels.
        };
    });