return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV99
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";

        public const string Find_01 = "    if (N == 24 && Route == \"/Apply/\")\r\n    {\r\n        var PendingDriver = Drivers.FirstOrDefault(D => D.Email == ResolvedActor);\r\n        await Wolfs.DbPutAsync(\"applicants\", new JsonObject\r\n        {\r\n            [\"id\"] = \"aud_scene_024_apply_prestate\",\r\n            [\"name\"] = PendingDriver.Name ?? ResolvedActor,\r\n            [\"email\"] = ResolvedActor,\r\n            [\"location\"] = PendingDriver.Location ?? \"\",\r\n            [\"experienceYears\"] = PendingDriver.Years,\r\n            [\"status\"] = \"pending\",\r\n            [\"note\"] = Narration,\r\n            [\"actor\"] = ResolvedActor,\r\n            [\"permission\"] = \"applicant.write\",\r\n        });\r\n    }\r\n";
        public const string Replace_01 = "    if (N == 24 && Route == \"/Apply/\")\r\n    {\r\n        var PendingDriver = Drivers.FirstOrDefault(D => D.Email == ResolvedActor);\r\n        await Wolfs.DbPutAsync(\"applicants\", new JsonObject\r\n        {\r\n            [\"id\"] = \"aud_scene_024_apply_prestate\",\r\n            [\"name\"] = PendingDriver.Name ?? ResolvedActor,\r\n            [\"email\"] = ResolvedActor,\r\n            [\"location\"] = PendingDriver.Location ?? \"\",\r\n            [\"experienceYears\"] = PendingDriver.Years,\r\n            [\"status\"] = \"pending\",\r\n            [\"note\"] = Narration,\r\n            [\"actor\"] = ResolvedActor,\r\n            [\"permission\"] = \"applicant.write\",\r\n        });\r\n    }\r\n\r\n    if (N == 38 && Route == \"/Apply/\")\r\n    {\r\n        var PendingDriver038 = Drivers.FirstOrDefault(D => D.Email == ResolvedActor);\r\n        await Wolfs.DbPutAsync(\"applicants\", new JsonObject\r\n        {\r\n            [\"id\"] = \"aud_scene_038_apply_prestate\",\r\n            [\"name\"] = PendingDriver038.Name ?? ResolvedActor,\r\n            [\"email\"] = ResolvedActor,\r\n            [\"location\"] = PendingDriver038.Location ?? \"\",\r\n            [\"experienceYears\"] = PendingDriver038.Years,\r\n            [\"status\"] = \"pending\",\r\n            [\"note\"] = Narration,\r\n            [\"actor\"] = ResolvedActor,\r\n            [\"permission\"] = \"applicant.write\",\r\n        });\r\n    }\r\n";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
