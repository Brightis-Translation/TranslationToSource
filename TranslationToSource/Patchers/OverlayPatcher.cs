using GoogleSheetsApiV4.Contract;
using TranslationToSource.Models.Patchers;
using TranslationToSource.Models.Patchers.Layout;
using TranslationToSource.Models.Sheets;
using TranslationToSource.Models.Texts;
using TranslationToSource.Patchers.Layout;
using TranslationToSource.Source;
using TranslationToSource.Texts;

namespace TranslationToSource.Patchers;

internal class OverlayPatcher
{
    protected TextParser TextParser { get; } = new();
    protected TextCalculator TextCalculator { get; } = new();

    public virtual async Task<string?> Patch(ISheetManager sheet, OverlayConfigData overlayConfig)
    {
        // Create text patches
        var patcher = new OvrTextPatcher();
        OvrPatchData? assemblyPatches = await patcher.CreatePatchDataAsync(sheet, overlayConfig);
        if (assemblyPatches == null)
            return null;

        // Create text patches layout
        List<OvrSectionData> sections = CreateSections(assemblyPatches);
        AppendUnusedSpace(sections, assemblyPatches, overlayConfig.UseUnlimitedSpace);

        var ovrPatchLayouter = new OvrPatchLayouter();
        OvrPatchLayoutData? layout = ovrPatchLayouter.Create(assemblyPatches, sections);
        if (layout == null)
        {
            Console.WriteLine("Text could not fit into the overlay!");
            return null;
        }

        // Emit patch source
        var sourceEmitter = new OverlayTextAssemblySourceEmitter();
        string source = sourceEmitter.EmitTextPatchSource(layout, $"OVR\\{overlayConfig.OverlaySlot:000}.bin");

        return source;
    }

    protected List<OvrSectionData> CreateSections(OvrPatchData patchData)
    {
        var result = new List<OvrSectionData>();

        OvrTextPatchData[] orderedPatches = patchData.TextPatches.OrderBy(p => p.SheetData.Offset).ToArray();

        var currentOffset = 0L;
        var currentLength = 0;
        for (var i = 0; i < orderedPatches.Length; i++)
        {
            IList<CharacterData> originalCharacters = TextParser.Parse(orderedPatches[i].SheetData.OriginalText);

            int originalLength = TextCalculator.CalculateByteLength(originalCharacters);
            originalLength = (originalLength + 3) & ~3;

            if (i == 0)
            {
                currentOffset = orderedPatches[i].SheetData.Offset;
                currentLength = originalLength;
            }
            else
            {
                if (currentOffset + currentLength != orderedPatches[i].SheetData.Offset)
                {
                    result.Add(new OvrSectionData
                    {
                        Offset = currentOffset,
                        Length = currentLength
                    });

                    currentOffset = orderedPatches[i].SheetData.Offset;
                    currentLength = originalLength;
                }
                else
                {
                    currentLength += originalLength;
                }
            }
        }

        result.Add(new OvrSectionData
        {
            Offset = currentOffset,
            Length = currentLength
        });

        return result;
    }

    protected void AppendUnusedSpace(List<OvrSectionData> sections, OvrPatchData patchData, bool unlimitedSpace = false)
    {
        OvrSectionData lastSection = sections[^1];

        long sectionEndOffset = lastSection.Offset + lastSection.Length;
        long overlayEndOffset = (patchData.OverlayRange.OverlayBaseAddress + patchData.OverlayRange.OverlaySize + 3) & ~3;
        long overlayMaxEndOffset = (patchData.OverlayRange.OverlayBaseAddress + patchData.OverlayRange.OverlayMaxSize + 3) & ~3;

        if (sectionEndOffset != overlayEndOffset)
        {
            sections.Add(new OvrSectionData
            {
                Offset = overlayEndOffset,
                Length = unlimitedSpace ? -1 : (int)(overlayMaxEndOffset - overlayEndOffset)
            });
        }
        else
        {
            sections[^1] = new OvrSectionData
            {
                Offset = sections[^1].Offset,
                Length = unlimitedSpace ? -1 : (int)(overlayMaxEndOffset - sections[^1].Offset)
            };
        }
    }
}