﻿using System.Diagnostics.CodeAnalysis;

namespace PTTGC.AskMeX.App.Core.Types;

public class WorkspaceFile
{
    /// <summary>
    /// file name with extension
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Thumbnail url of the file which may not exist.
    /// </summary>
    public required string ThumbnailUrl { get; init; }

    /// <summary>
    /// The content type of the file. Can only be ether ".pdf" or ".xlsx".
    /// </summary>
    public required string FileExtension { get; init; }

    public required string BlobName { get; init; }
}

public class WorkspaceFileBlobNameComparer : IEqualityComparer<WorkspaceFile>
{
    public bool Equals(WorkspaceFile? x, WorkspaceFile? y)
    {
        return x!.BlobName == y!.BlobName;
    }

    public int GetHashCode([DisallowNull] WorkspaceFile obj)
    {
        return obj.BlobName.GetHashCode();
    }
}
