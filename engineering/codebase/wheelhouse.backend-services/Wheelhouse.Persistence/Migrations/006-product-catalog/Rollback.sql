-- Reverse 006-product-catalog — drop the recorded lifecycles and the integration keys. Dev/test recovery only: every
-- key stops working and each product reads as building again. The legacy products registry was never changed.

DROP TABLE IF EXISTS integration_keys;
DROP TABLE IF EXISTS product_metadata;
