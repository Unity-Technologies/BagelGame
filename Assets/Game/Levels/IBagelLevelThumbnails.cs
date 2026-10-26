using UnityEngine;

namespace Bagel
{
    // Live pictures of the library's pieces, each turning slowly on its own, and turned by hand when spun: all in one
    // texture, a part of it for each (BagelLevelStudio).
    public interface IBagelLevelThumbnails
    {
        Texture texture { get; }

        // The part of the texture a piece is in (texture coordinates, from the bottom left).
        Rect UV(string kind);

        // Turned by hand, this many degrees (clockwise from above); it goes on turning a little, then slowly.
        void Spin(string kind, float degrees);
    }
}
