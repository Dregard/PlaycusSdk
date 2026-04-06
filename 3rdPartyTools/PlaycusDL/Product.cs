using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace PlaycusDL {

    public class Product<T> : Params where T : Product<T> {

        private List<Dictionary<string, object>> virtualCurrencies;
        private List<Dictionary<string, object>> items;

        public T SetRealCurrency(string type, int amount)
        {
            if (String.IsNullOrEmpty(type)) {
                throw new ArgumentException("Type cannot be null or empty");
            }

            var realCurrency = new Dictionary<string, object>() {
                { "realCurrencyType", type },
                { "realCurrencyAmount", amount }
            };

            AddParam("realCurrency", realCurrency);

            return (T) this;
        }

        public T AddVirtualCurrency(string name, string type, long amount)
        {
            if (String.IsNullOrEmpty(name)) {
                throw new ArgumentException("Name cannot be null or empty");
            }

            if (String.IsNullOrEmpty(type)) {
                throw new ArgumentException("Type cannot be null or empty");
            }

            var virtualCurrency = new Dictionary<string, object>() {
                { "virtualCurrency", new Dictionary<string, object>() {
                        { "virtualCurrencyName", name },
                        { "virtualCurrencyType", type },
                        { "virtualCurrencyAmount", amount }
                    }
                }
            };

            if (GetParam("virtualCurrencies") == null) {
                this.virtualCurrencies = new List<Dictionary<string, object>>();
                AddParam("virtualCurrencies", this.virtualCurrencies);
            }

            this.virtualCurrencies.Add(virtualCurrency);

            return (T) this;
        }

        public T AddItem(string name, string type, string placement, int amount)
        {
            if (String.IsNullOrEmpty(name)) {
                throw new ArgumentException("Name cannot be null or empty");
            }

            if (String.IsNullOrEmpty(type)) {
                throw new ArgumentException("Type cannot be null or empty");
            }

            var item = new Dictionary<string, object>() {
                { "item", new Dictionary<string, object>() {
                        { "itemName", name },
                        { "itemType", type },
                        { "pr_placement", placement },
                        { "itemAmount", amount }
                    }
                }
            };

            if (GetParam("items") == null) {
                this.items = new List<Dictionary<string, object>>();
                AddParam("items", this.items);
            }

            this.items.Add(item);

            return (T) this;
        }

        /// <summary>
        /// Converts a currency in a decimal format, such as '1.23' EUR, into an
        /// integer representation which can be used with the SetRealCurrency method.
        /// This method will also work for currencies which don't use a minor currency
        /// unit, for example such as the Japanese Yen (JPY).
        /// </summary>
        /// <param name="code">The ISO 4217 currency code</param>
        /// <param name="value">The currency value to convert</param>
        /// <returns>The converted integer value</returns>
        /// <seealso cref="SetRealCurrency(string, int)"/>
        public static int ConvertCurrency(string code, decimal value) {
            if (ISO4217.ContainsKey(code)) {
                return decimal.ToInt32(value * (decimal) Math.Pow(10, ISO4217[code]));
            } else {
                Debug.LogWarning("Failed to find currency for: " + code);
                return 0;
            }
        }

        private static readonly IDictionary<string, int> ISO4217;
        static Product() {
            ISO4217 = new Dictionary<string, int>();

            using (XmlReader reader = XmlReader.Create(new StringReader((Resources.Load(
                    "iso_4217",
                    typeof(TextAsset)) as TextAsset).text))) {
                bool expectingCode = false;
                bool expectingValue = false;
                string pulledCode = null;
                string pulledValue = null;

                while (reader.Read()) {
                    switch (reader.NodeType) {
                        case XmlNodeType.Element:
                            if (reader.Name.Equals("Ccy")) {
                                expectingCode = true;
                            } else if (reader.Name.Equals("CcyMnrUnts")) {
                                expectingValue = true;
                            }
                            break;

                        case XmlNodeType.Text:
                            if (expectingCode) {
                                pulledCode = reader.Value;
                            } else if (expectingValue) {
                                pulledValue = reader.Value;
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if (reader.Name.Equals("Ccy")) {
                                expectingCode = false;
                            } else if (reader.Name.Equals("CcyMnrUnts")) {
                                expectingValue = false;
                            } else if (reader.Name.Equals("CcyNtry")) {
                                if (!string.IsNullOrEmpty(pulledCode)
                                    && !string.IsNullOrEmpty(pulledValue)) {
                                    int value;
                                    try {
                                        value = int.Parse(pulledValue);
                                    } catch (FormatException) {
                                        value = 0;
                                    }

                                    ISO4217[pulledCode] = value;
                                }

                                expectingCode = false;
                                expectingValue = false;
                                pulledCode = null;
                                pulledValue = null;
                            }
                            break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Creates a Product, which helps build Transaction and Achievement events.
    /// </summary>
    public class ProductPDL : Product<ProductPDL> {}

} // namespace PlaycusDL
