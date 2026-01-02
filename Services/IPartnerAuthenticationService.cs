namespace TransactionApi.Services
{
    public interface IPartnerAuthenticationService
    {
        bool ValidatePartner(string partnerKey, string partnerRefNo, string encodedPassword);
        string GetPartnerPassword(string partnerKey, string partnerRefNo);
    }
}

