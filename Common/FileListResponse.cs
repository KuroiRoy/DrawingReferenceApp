﻿namespace Common;

public class FileListResponse {
    public string Path { get; set; }
    public List<string> Folders { get; set; }
    public List<string> Files { get; set; }
}