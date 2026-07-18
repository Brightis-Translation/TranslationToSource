using System.Text;
using TranslationToSource.Models.Patchers.Layout;
using TranslationToSource.Models.Sheets;
using TranslationToSource.Models.Source;
using TranslationToSource.Models.Source.Instructions;

namespace TranslationToSource.Source;

internal class OverlayTextAssemblySourceEmitter : PsxAssemblySourceEmitter
{
    public string EmitTextPatchSource(OvrPatchLayoutData patchLayout, string origFileName)
    {
        var result = new StringBuilder();

        string architectureSource = Emit(new ArchitectureInstruction(ArmipsArchitecture.Psx));
        string openSource = Emit(new OpenInstruction(origFileName, patchLayout.OverlayRange.OverlayBaseAddress));

        result.AppendLine(architectureSource);
        result.AppendLine(openSource);

        result.AppendLine();

        foreach (OvrTextPatchLayoutData textPatch in patchLayout.TextPatches)
        {
            // Patch data offset instructions
            foreach (long dataOffset in textPatch.Patch.SheetData.DataOffsets)
            {
                string offsetSource = Emit(new SourceOffsetInstruction(dataOffset));

                string pointerPatchSource;
                switch (textPatch.Patch.SheetData.OverlayTextType ?? patchLayout.Config.OverlayTextType)
                {
                    case OverlayTextType.Inline:
                        // Patches addiu immediate value without changing registers and conditions
                        pointerPatchSource = Emit(new HalfWordsInstruction([(short)(textPatch.Offset - 0x80160000)]));
                        break;

                    case OverlayTextType.Pointer:
                        pointerPatchSource = Emit(new WordsInstruction([textPatch.Offset]));
                        break;

                    default:
                        continue;
                }

                result.AppendLine(offsetSource);
                result.AppendLine($"\t{pointerPatchSource}");
            }

            // Patch text blob
            string offsetSource1 = Emit(new SourceOffsetInstruction(textPatch.Offset));
            result.AppendLine(offsetSource1);

            foreach (ArmipsInstruction instruction in textPatch.Patch.Patch.Instructions)
                result.AppendLine($"\t{Emit(instruction)}");

            result.AppendLine();
        }

        result.Append(Emit(new CloseInstruction()));

        return result.ToString();
    }
};
