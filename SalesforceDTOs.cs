using SalesforceMagic.Attributes;
using SalesforceMagic.Entities;

namespace SalesforceBackupFilesDownloader
{

    [SalesforceName("Organization")]
    public class Organization:SObject
    {
        [SalesforceName("Id")]
        public string Id { get; set; }

        [SalesforceName("Name")]
        public string Name { get; set; }
    }
}
