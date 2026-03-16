using Newtonsoft.Json;

public class CreateVendorRequest
{
    [JsonProperty("vendor_name")]
    public string VendorName { get; set; }= string.Empty;

    [JsonProperty("vendor_email")]
    public string VendorEmail { get; set; } = string.Empty;

    [JsonProperty("vendor_phone_country")]
    public string VendorPhoneCountry { get; set; } = string.Empty;

    [JsonProperty("vendor_phone")]
    public string VendorPhone { get; set; } = string.Empty;

    [JsonProperty("vendor_location")]
    public string VendorLocation { get; set; } = string.Empty;

    [JsonProperty("vendor_address")]
    public string VendorAddress { get; set; } = string.Empty;

    [JsonProperty("vendor_sub_division")]
    public string VendorSubDivision { get; set; } = string.Empty;

    [JsonProperty("vendor_city")]
    public string VendorCity { get; set; } = string.Empty;

    [JsonProperty("vendor_postal_code")]
    public string VendorPostalCode { get; set; } = string.Empty;

    [JsonProperty("person_type")]
    public string PersonType { get; set; } = string.Empty;

    [JsonProperty("metadata")]
    public object Metadata { get; set; } = new object();
}
public class UpdateVendorRequest
{
    [JsonProperty("vendor_name")]
    public string VendorName { get; set; } = string.Empty;

    [JsonProperty("vendor_email")]
    public string VendorEmail { get; set; } = string.Empty;

    [JsonProperty("vendor_phone_country")]
    public string VendorPhoneCountry { get; set; } = string.Empty;  

    [JsonProperty("vendor_phone")]
    public string VendorPhone { get; set; } = string.Empty;

    [JsonProperty("vendor_location")]
    public string VendorLocation { get; set; } = string.Empty;

    [JsonProperty("vendor_address")]
    public string VendorAddress { get; set; } = string.Empty;
    
    [JsonProperty("vendor_sub_division")]
    public string VendorSubDivision { get; set; } = string.Empty;

    [JsonProperty("vendor_city")]
    public string VendorCity { get; set; } = string.Empty;

    [JsonProperty("vendor_postal_code")]
    public string VendorPostalCode { get; set; } = string.Empty;

    [JsonProperty("person_type")]
    public string PersonType { get; set; } = string.Empty;

    [JsonProperty("metadata")]
    public object Metadata { get; set; } = new object();
}
public class VendorPaymentMethodsRequest
{
    [JsonProperty("X-API-KEY")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonProperty("CLIENT_ID")]
    public string ClientId { get; set; } = string.Empty;
}
public class NewVendorAccountRequest
{
    [JsonProperty("pay_account_name")]
    public string PayAccountName { get; set; } = string.Empty;

    [JsonProperty("pay_method_type")]
    public string PayMethodType { get; set; } = string.Empty;

    [JsonProperty("client_vendor_id")]
    public string ClientVendorId { get; set; } = string.Empty;

    [JsonProperty("metadata")]
    public Metadata Metadata { get; set; } = new Metadata();
}


public class Metadata
{
    [JsonProperty("beneficiary_name")]
    public string BeneficiaryName { get; set; } = string.Empty;

    [JsonProperty("routing_number")]
    public string RoutingNumber { get; set; } = string.Empty;

    [JsonProperty("account_number")]
    public string AccountNumber { get; set; } = string.Empty;
}
public class PayVendorRequest
{
    [JsonProperty("payment_account_id")]
    public string PaymentAccountId { get; set; } = string.Empty;

    [JsonProperty("amount")]
    public decimal Amount { get; set; } = 0;

    [JsonProperty("pay_currency")]
    public string PayCurrency { get; set; } = string.Empty;

    [JsonProperty("source_currency")]
    public string SourceCurrency { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonProperty("external_memo")]
    public string ExternalMemo { get; set; } = string.Empty;

    [JsonProperty("metadata")]
    public PayMetadata metadata { get; set; } = new PayMetadata();
}

public class PayMetadata
{
    [JsonProperty("purpose")]
    public string Purpose { get; set; } = string.Empty;
}