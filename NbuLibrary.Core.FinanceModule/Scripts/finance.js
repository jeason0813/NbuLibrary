define('mods/finance',
    ['router', 'presenter', 'dataservice', 'usercontext', 'entitypage', 'textprovider', 'home', 'mods/vm.payments'],
    function (router, presenter, dataservice, usercontext, ep, tp, home, vmPayments) {

        var page = new ep.Page();
        page.template = 'GeneralDetails';

        router.add('#/finance/payments/:id', function () {
            var ui = usercontext.isCustomer() ? 'Finance_Customer_PaymentDetails' : 'Finance_Librarian_PaymentDetails';
            page.bind('Payment', ui, this.id);
            page.load();
            presenter.show(page);
        });

        router.add('#/finance/pendingPayments/', function () {
            vmPayments.bind(true);
            presenter.show(vmPayments);
        });

        router.add('#/finance/payments/', function () {
            vmPayments.bind(false);
            presenter.show(vmPayments);
        });


        var qp = dataservice.search('Payment');
        qp.rules.is('Status', 1);//pending
        qp.rules.relatedTo('User', 'Customer', usercontext.currentUser().Id);
        $.when(qp.execute()).done(function (raw) {
            if (raw.length > 0)
                home.addInfo(tp.get('Неплатени задължения'), 'Имате <a href="#/finance/pendingPayments/"> неплатени задължения (' + raw.length + ')</a>.', 'briefcase');
        });
    });