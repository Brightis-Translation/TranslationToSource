namespace TranslationToSource.Models.Source.Instructions;

internal record HalfWordsInstruction(short[] Values) : ArmipsInstruction;