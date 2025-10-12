using Diz.Core.Interfaces;
using Diz.Core.model;
using Diz.Cpu._65816;

namespace Diz.Ui.Winforms.window;

public partial class MainWindow
{
    private int FindIntermediateAddress(int offset)
    {
        if (!RomDataPresent())
            return -1;

        var ia = Project.Data.GetSnesApi().GetIntermediateAddressOrPointer(offset);
        if (ia < 0)
            return -1;

        return Project.Data.ConvertSnesToPc(ia);
    }

    private bool FindUnreached(int offset, bool fromEnd, bool directionIsForward, out int unreached, bool anyBoundaryCrossed = true)
    {
        var size = Project.Data.GetRomSize();
        
        // pick a starting point
        if (fromEnd)
            unreached = directionIsForward 
                ? 0             // start of rom 
                : size - 1;     // end of rom
        else
            unreached = offset; // just use the current position

        // starting position is out of bounds
        if (unreached < 0 || unreached >= size)
            return false;
        
        var startingIsUnreached = IsUnreached(unreached);
        bool IsUnreached2(int offsetToCheck) // TODO: rename
        {
            // original behavior: only takes us to regions that are actually unreached
            if (!anyBoundaryCrossed)
                return IsUnreached(offsetToCheck);
            
            // new behavior: takes us to regions where the unreached flag changed from search's starting position
            return IsUnreached(offsetToCheck) != startingIsUnreached;
        }
        
        // STEP 1
        // search (in whatever direction) to get our offset in any part of the "next unreached" area
        // we're done this step when either:
        // - we hit a byte of unreached data that we are calling the target, or,
        // - we ran out of ROM to search and there's no more "next" unreached past our point
        if (directionIsForward)
        {
            // if going forward from where we are, and we're already in an unreached area, go find the next REACHED area
            if (!fromEnd)
                while (unreached < size - 1 && IsUnreached2(unreached))
                    unreached++;
                
            // we're in a reached area. now keep going PAST this until we hit a byte that's unreached
            while (unreached < size - 1 && !IsUnreached2(unreached)) 
                unreached++;
        }
        else
        {
            // start by going backwards on byte from starting point (if in range)
            if (unreached > 0) 
                unreached--;
                
            // keep going backwards until we hit an unreached byte
            while (unreached > 0 && !IsUnreached2(unreached)) 
                unreached--;
        }

        // STEP 2
        // we landed on the target region we're saying is the "next unreached" (or, we're out of bounds)
        // now, either we're right at the beginning, or, right at the end of it, depending on our search direction above.
        // regardless of the direction we WERE searching, get us to the START of this region.
        // if we're going forward, this won't do anything (we're already there)
        // if we're going backwards, it will take us to the start.
        while (unreached > 0 && IsUnreached2(unreached - 1)) 
            unreached--;

        // if we ended up on an unreached byte, we were successful. if not, either there aren't any, or, we ran off the
        // end of the rom and there isn't a "next" unreached area.
        return IsUnreached2(unreached);
    }

    private bool IsUnreached(int offset)
    {
        return Project.Data.GetSnesApi().GetFlag(offset) == FlagType.Unreached;
    }

    private bool RomDataPresent()
    {
        return Project?.Data?.GetRomSize() > 0;
    }

    private bool IsOffsetInRange(int offset)
    {
        return offset >= 0 && offset < Project.Data.GetRomSize();
    }
}