CREATE OR REPLACE FUNCTION production.product_update_notify()
    RETURNS trigger
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
  Id integer;
  Name character varying(512);
BEGIN
  IF TG_OP = 'INSERT' OR TG_OP = 'UPDATE' THEN
    Id = NEW."productid";
    Name = NEW."name";
  ELSE
    Id = OLD."productid";
    Name = OLD."name";
  END IF;
  PERFORM pg_notify('notification_production_product', TG_OP || ';' || Id || ';' || Name);
  RETURN NEW;
END;

$BODY$;

ALTER FUNCTION production.product_update_notify()
    OWNER TO superman;


-------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.notify_data_change()
    RETURNS trigger
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE NOT LEAKPROOF
AS $BODY$
 
DECLARE 
  data JSON;
  notification JSON;
BEGIN
  -- if we delete, then pass the old data
  -- if we insert or update, pass the new data
  IF (TG_OP = 'DELETE') THEN
    data = row_to_json(OLD);
  ELSE
    data = row_to_json(NEW);
  END IF;

  -- create json payload
  -- note that here can be done projection 
  notification = json_build_object(
            'schema', TG_TABLE_SCHEMA,
            'table',TG_TABLE_NAME,
            'action', TG_OP, -- can have value of INSERT, UPDATE, DELETE
            'data', data);  
            
    -- note that channel name MUST be lowercase, otherwise pg_notify() won't work
    PERFORM pg_notify('datachange', notification::TEXT);
  RETURN NEW;
END
$BODY$;

ALTER FUNCTION public.notify_data_change()
    OWNER TO superman;
