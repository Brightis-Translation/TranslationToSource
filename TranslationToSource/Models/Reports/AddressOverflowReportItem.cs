namespace TranslationToSource.Models.Reports;

record AddressOverflowReportItem(long OriginalAddress, bool IsOriginalUpperHalf, bool IsPatchedUpperHalf) : ReportItem
{
    public override string Serialize()
    {
        return $"Text address 0x{OriginalAddress:X8} changed from {(IsOriginalUpperHalf ? ">=" : "<")}0x8000 to {(IsPatchedUpperHalf ? ">=" : "<")}0x8000.";
    }
}