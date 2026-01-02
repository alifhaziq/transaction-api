using System.Text;

namespace TransactionApi.Services
{
    public class PartnerAuthenticationService : IPartnerAuthenticationService
    {
        private readonly Dictionary<string, PartnerInfo> _allowedPartners;

        public PartnerAuthenticationService()
        {
            // Initialize allowed partners
            _allowedPartners = new Dictionary<string, PartnerInfo>
            {
                { "FG-00001", new PartnerInfo { PartnerNo = "FG-00001", PartnerKey = "FAKEGOOGLE", Password = "FAKEPASSWORD1234" } },
                { "FG-00002", new PartnerInfo { PartnerNo = "FG-00002", PartnerKey = "FAKEPEOPLE", Password = "FAKEPASSWORD4578" } }
            };
        }

        public bool ValidatePartner(string partnerKey, string partnerRefNo, string encodedPassword)
        {
            if (string.IsNullOrWhiteSpace(partnerKey) || string.IsNullOrWhiteSpace(partnerRefNo) || string.IsNullOrWhiteSpace(encodedPassword))
                return false;

            if (!_allowedPartners.TryGetValue(partnerRefNo, out var partner))
                return false;

            if (partner.PartnerKey != partnerKey)
                return false;

            // Decode the password and compare
            try
            {
                var decodedPassword = Encoding.UTF8.GetString(Convert.FromBase64String(encodedPassword));
                return decodedPassword == partner.Password;
            }
            catch
            {
                return false;
            }
        }

        public string GetPartnerPassword(string partnerKey, string partnerRefNo)
        {
            if (_allowedPartners.TryGetValue(partnerRefNo, out var partner) && partner.PartnerKey == partnerKey)
            {
                return partner.Password;
            }
            return null;
        }

        private class PartnerInfo
        {
            public string PartnerNo { get; set; }
            public string PartnerKey { get; set; }
            public string Password { get; set; }
        }
    }
}

