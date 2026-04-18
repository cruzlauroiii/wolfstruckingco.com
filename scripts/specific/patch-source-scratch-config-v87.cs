return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV87
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";

        public const string Find_01 = "    await PerformSceneCrud(N, Route, Narration, RouteOrd, Wolfs, Drivers, AdminEmail, EmployerEmail, BuyerEmail, ResolvedActor, Inputs, SsoProvider);\r\n    FormFills.Add(Inputs);\r\n\r\n    if (!PagesByRoute.TryGetValue(Route, out var PageType))";
        public const string Replace_01 = "    await PerformSceneCrud(N, Route, Narration, RouteOrd, Wolfs, Drivers, AdminEmail, EmployerEmail, BuyerEmail, ResolvedActor, Inputs, SsoProvider);\r\n    FormFills.Add(Inputs);\r\n\r\n    if (N == 24 && Route == \"/Apply/\")\r\n    {\r\n        var PendingDriver = Drivers.FirstOrDefault(D => D.Email == ResolvedActor);\r\n        await Wolfs.DbPutAsync(\"applicants\", new JsonObject\r\n        {\r\n            [\"id\"] = \"aud_scene_024_apply_prestate\",\r\n            [\"name\"] = PendingDriver.Name ?? ResolvedActor,\r\n            [\"email\"] = ResolvedActor,\r\n            [\"location\"] = PendingDriver.Location ?? \"\",\r\n            [\"experienceYears\"] = PendingDriver.Years,\r\n            [\"status\"] = \"pending\",\r\n            [\"note\"] = Narration,\r\n            [\"actor\"] = ResolvedActor,\r\n            [\"permission\"] = \"applicant.write\",\r\n        });\r\n    }\r\n\r\n    if (!PagesByRoute.TryGetValue(Route, out var PageType))";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
