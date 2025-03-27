using Spectre.Console;

namespace MediaKiller;

class CxSpinner : Spinner
{
    public override TimeSpan Interval => TimeSpan.FromMilliseconds(200);

    public override bool IsUnicode => true;

    public override IReadOnlyList<string> Frames => [
        "···",
        " •··",
        "  •··",
        "·  •·",
        "··  •"
        ];
}
