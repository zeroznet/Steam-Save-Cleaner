using SteamKit2;
using SteamKit2.Internal;

namespace Ssc;

public sealed record DeleteResult(CloudApp App, int Deleted, int Failed, int Remaining, string? Error);

public sealed class CloudFiles(SteamSession session, TextWriter log)
{
    private const uint PageSize = 500;

    public async Task<IReadOnlyList<CCloud_UserFile>> ListAsync(uint appId) =>
        await Paging.CollectAsync<CCloud_UserFile>(async start =>
        {
            var response = await session.Cloud.EnumerateUserFiles(new CCloud_EnumerateUserFiles_Request
            {
                appid = appId,
                extended_details = true,
                count = PageSize,
                start_index = start,
            });
            if (response.Result != EResult.OK)
            {
                throw new InvalidOperationException($"EnumerateUserFiles {appId}: {response.Result}");
            }
            return (response.Body.files, response.Body.total_files);
        });

    public async Task<DeleteResult> DeleteAppAsync(CloudApp app)
    {
        try
        {
            var files = await ListAsync(app.AppId);
            if (files.Count == 0)
            {
                return new DeleteResult(app, 0, 0, 0, null);
            }
            var pending = files.Select(file => file.filename).ToHashSet();
            await DeleteInUploadBatchAsync(app.AppId, pending);
            await DeletePlainAsync(app.AppId, pending);
            var remaining = (await ListAsync(app.AppId)).Count;
            return new DeleteResult(app, files.Count - remaining, pending.Count, remaining, null);
        }
        catch (Exception e) when (e is InvalidOperationException or AsyncJobFailedException or TaskCanceledException)
        {
            return new DeleteResult(app, 0, 0, (int)app.FileCount, e.Message);
        }
    }

    // The sequence the Steam client uses; removes each deleted name from pending.
    private async Task DeleteInUploadBatchAsync(uint appId, HashSet<string> pending)
    {
        var begin = new CCloud_BeginAppUploadBatch_Request { appid = appId, machine_name = Environment.MachineName };
        begin.files_to_delete.AddRange(pending);
        var batch = await session.Cloud.BeginAppUploadBatch(begin);
        if (batch.Result != EResult.OK)
        {
            log.WriteLine($"{appId}: upload batch refused ({batch.Result}), deleting file by file.");
            return;
        }
        var batchResult = EResult.OK;
        foreach (var name in pending.ToList())
        {
            var deleted = await session.Cloud.ClientDeleteFile(new CCloud_ClientDeleteFile_Request
            {
                appid = appId,
                filename = name,
                is_explicit_delete = true,
                upload_batch_id = batch.Body.batch_id,
            });
            if (deleted.Result == EResult.OK)
            {
                pending.Remove(name);
            }
            else if (batchResult == EResult.OK)
            {
                batchResult = deleted.Result;
            }
        }
        await session.Cloud.CompleteAppUploadBatchBlocking(new CCloud_CompleteAppUploadBatch_Request
        {
            appid = appId,
            batch_id = batch.Body.batch_id,
            batch_eresult = (uint)batchResult,
        });
    }

    private async Task DeletePlainAsync(uint appId, HashSet<string> pending)
    {
        foreach (var name in pending.ToList())
        {
            var deleted = await session.Cloud.Delete(new CCloud_Delete_Request { appid = appId, filename = name });
            if (deleted.Result == EResult.OK)
            {
                pending.Remove(name);
            }
        }
    }
}
