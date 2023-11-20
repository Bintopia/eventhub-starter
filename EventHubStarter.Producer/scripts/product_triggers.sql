CREATE TRIGGER product_notify_delete
    AFTER DELETE
    ON production."product"
    FOR EACH ROW
    EXECUTE PROCEDURE production."product_update_notify"();

CREATE TRIGGER product_notify_insert
    AFTER INSERT
    ON production."product"
    FOR EACH ROW
    EXECUTE PROCEDURE production."product_update_notify"();

CREATE TRIGGER product_notify_update
    AFTER UPDATE 
    ON production."product"
    FOR EACH ROW
    EXECUTE PROCEDURE production."product_update_notify"();

------------------------------------------------------------------------------------------
-- https://www.graymatterdeveloper.com/2019/12/02/listening-events-postgresql/index.html

CREATE TRIGGER "OnDataChange"
  AFTER INSERT OR DELETE OR UPDATE 
  ON production.product
  FOR EACH ROW
  EXECUTE PROCEDURE public."notify_data_change"();