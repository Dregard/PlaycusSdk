using System;

namespace PlaycusDL {

    public class Transaction<T> : GameEvent<T> where T : Transaction<T> {

        public Transaction(string name, string type, ProductPDL productsReceived, ProductPDL productsSpent)
        :
            base("transaction")
        {
            if (String.IsNullOrEmpty(name)) {
                throw new ArgumentException("Name cannot be null or empty");
            }

            if (String.IsNullOrEmpty(type)) {
                throw new ArgumentException("Type cannot be null or empty");
            }

            if (productsReceived == null) {
                throw new ArgumentException("Products received cannot be null");
            }

            if (productsSpent == null) {
                throw new ArgumentException("Products spent cannot be null");
            }

            AddParam("transactionName", name);
            AddParam("transactionType", type);
            AddParam("productsReceived", productsReceived);
            AddParam("productsSpent", productsSpent);
        }

        public T SetTransactionId(string transactionId)
        {
            if (String.IsNullOrEmpty(transactionId)) {
                throw new ArgumentException("transactionId cannot be null or empty");
            }

            AddParam("transactionID", transactionId);
            return (T) this;
        }

        public T SetReceipt(string receipt)
        {
            if (String.IsNullOrEmpty(receipt)) {
                throw new ArgumentException("receipt cannot be null or empty");
            }

            AddParam("transactionReceipt", receipt);
            return (T) this;
        }

        public T SetReceiptSignature(string receiptSignature) {
            if (string.IsNullOrEmpty(receiptSignature)) {
                throw new ArgumentException("receipt signature cannot be null or empty");
            }

            AddParam("transactionReceiptSignature", receiptSignature);
            return (T) this;
        }

        public T SetServer(string server)
        {
            if (String.IsNullOrEmpty(server)) {
                throw new ArgumentException("server cannot be null or empty");
            }

            AddParam("transactionServer", server);
            return (T) this;
        }

        public T SetTransactorId(string transactorId)
        {
            if (String.IsNullOrEmpty(transactorId)) {
                throw new ArgumentException("transactorId cannot be null or empty");
            }

            AddParam("transactorID", transactorId);
            return (T) this;
        }

        public T SetProductId(string productId)
        {
            if (String.IsNullOrEmpty(productId)) {
                throw new ArgumentException("productId cannot be null or empty");
            }
            AddParam("productID", productId);
            return (T) this;
        }
    }

    /// <summary>
    /// Creates a Transaction event.
    /// </summary>
    public class TransactionPDL : Transaction<TransactionPDL> {

        public TransactionPDL(string name, string type, ProductPDL productsReceived, ProductPDL productsSpent)
        : base(name, type, productsReceived, productsSpent) {}
    }

} // namespace PlaycusDL
