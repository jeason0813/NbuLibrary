using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Domain
{
    public class Payment : Entity
    {
        public const string ENTITY = "Payment";
        public const string ROLE_CUSTOMER = "Customer";

        public Payment()
            : base(ENTITY)
        {

        }

        public Payment(int id)
            : base(ENTITY, id)
        {

        }

        public Payment(Entity e)
            : base(e)
        {

        }

        public decimal Amount
        {
            get
            {
                return GetData<decimal>("Amount");
            }
            set
            {
                SetData<decimal>("Amount", value);
            }
        }
        public PaymentStatus Status
        {
            get
            {
                return GetData<PaymentStatus>("Status");
            }
            set
            {
                SetData<PaymentStatus>("Status", value);
            }
        }
        public PaymentMethod Method
        {
            get
            {
                return GetData<PaymentMethod>("Method");
            }
            set
            {
                SetData<PaymentMethod>("Method", value);
            }
        }
    }

    public enum PaymentStatus
    {
        None = 0,
        Pending = 1,
        Paid = 2
    }

    public enum PaymentMethod
    {
        Cash = 0,
        CardCredit = 1,
        ReaderRecord = 2,
        BankTransfer = 3
    }
}
