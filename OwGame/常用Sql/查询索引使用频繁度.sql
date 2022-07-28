SELECT  objects.name ,

        databases.name ,

        indexes.name ,

        user_seeks ,

        user_scans ,

        user_lookups ,

        partition_stats.row_count

FROM    sys.dm_db_index_usage_stats stats

        LEFT JOIN sys.objects objects ON stats.object_id = objects.object_id

        LEFT JOIN sys.databases databases ON databases.database_id = stats.database_id

        LEFT JOIN sys.indexes indexes ON indexes.index_id = stats.index_id

                                         AND stats.object_id = indexes.object_id

        LEFT  JOIN sys.dm_db_partition_stats partition_stats ON stats.object_id = partition_stats.object_id

                                                              AND indexes.index_id = partition_stats.index_id

WHERE   1 = 1

--AND databases.database_id = 7

        AND objects.name IS NOT NULL

        AND indexes.name IS NOT NULL

        AND user_scans>0

ORDER BY user_scans DESC ,

        stats.object_id ,

        indexes.index_id;

select * from master.dbo.sysprocesses where dbid = DB_ID('GY2021001Dev') --取活动链接数量
