delete from [GameItems] where [OwnerId] is not null and not exists (
SELECT 1 FROM [dbo].[GameChars] as t1  where t1.[Id]=[GameItems].[OwnerId])
go

delete from [GameItems] where [OwnerId] is null and not exists (
SELECT 1 FROM [dbo].[GameItems] as t1  where t1.[Id]=[GameItems].[ParentId])
go

while(select @@ROWCOUNT)>0
begin
delete from [GameItems] where [OwnerId] is null and not exists (
SELECT 1 FROM [dbo].[GameItems] as t1  where t1.[Id]=[GameItems].[ParentId])
end
go

DELETE FROM [dbo].[ExtendProperties] where not exists (select 1 from [GameItems] as t1 where t1.Id=[ExtendProperties].Id) and not exists
(select 1 from GameChars as t2 where t2.Id=[ExtendProperties].Id) and not exists
(select 1 from GameUsers as t3 where t3.Id=[ExtendProperties].Id)
go