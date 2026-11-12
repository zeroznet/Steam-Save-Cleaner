// SPDX-FileCopyrightText: Copyright 2026 Robert Bopko
// SPDX-License-Identifier: BSD-2-Clause

namespace Ssc;

public sealed record CloudApp(uint AppId, string Name, uint FileCount, ulong TotalSize);
