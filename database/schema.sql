-- Projeto Mensageria - esquema relacional (PostgreSQL)
-- Os totais (pedido e item) NÃO são armazenados: são calculados dinamicamente pela API.

CREATE TABLE cliente (
    id        BIGINT       PRIMARY KEY,
    name      VARCHAR(200) NOT NULL,
    email     VARCHAR(200),
    document  VARCHAR(30)
);

CREATE TABLE seller (
    id     BIGINT       PRIMARY KEY,
    name   VARCHAR(200) NOT NULL,
    city   VARCHAR(100),
    state  VARCHAR(2)
);

-- category e sub_category ficam na mesma tabela (auto-relacionamento)
CREATE TABLE categoria (
    id         VARCHAR(50)  PRIMARY KEY,
    name       VARCHAR(100) NOT NULL,
    parent_id  VARCHAR(50)  REFERENCES categoria (id)
);

CREATE TABLE produto (
    id     VARCHAR(50)  PRIMARY KEY,
    title  VARCHAR(255) NOT NULL
);

CREATE TABLE pedido (
    uuid         VARCHAR(50)  PRIMARY KEY,
    created_at   TIMESTAMPTZ  NOT NULL,
    channel      VARCHAR(50),
    status       VARCHAR(20)  NOT NULL,
    customer_id  BIGINT       NOT NULL REFERENCES cliente (id),
    seller_id    BIGINT       NOT NULL REFERENCES seller (id),
    indexed_at   TIMESTAMPTZ  NOT NULL DEFAULT now()  -- hora em que a mensagem foi indexada
);

CREATE INDEX ix_pedido_created_at  ON pedido (created_at);
CREATE INDEX ix_pedido_customer_id ON pedido (customer_id);
CREATE INDEX ix_pedido_seller_id   ON pedido (seller_id);
CREATE INDEX ix_pedido_status      ON pedido (status);

CREATE TABLE item_pedido (
    order_uuid       VARCHAR(50)    NOT NULL REFERENCES pedido (uuid) ON DELETE CASCADE,
    id               INT            NOT NULL,
    product_id       VARCHAR(50)    NOT NULL REFERENCES produto (id),
    category_id      VARCHAR(50)    REFERENCES categoria (id),
    sub_category_id  VARCHAR(50)    REFERENCES categoria (id),
    unit_price       NUMERIC(12, 2) NOT NULL CHECK (unit_price >= 0),
    quantity         INT            NOT NULL CHECK (quantity > 0),
    PRIMARY KEY (order_uuid, id)
);

CREATE INDEX ix_item_pedido_product_id ON item_pedido (product_id);

CREATE TABLE pagamento (
    order_uuid      VARCHAR(50) PRIMARY KEY REFERENCES pedido (uuid) ON DELETE CASCADE,
    method          VARCHAR(30) NOT NULL,
    status          VARCHAR(30),
    transaction_id  VARCHAR(100)
);

CREATE TABLE envio (
    order_uuid     VARCHAR(50) PRIMARY KEY REFERENCES pedido (uuid) ON DELETE CASCADE,
    carrier        VARCHAR(100),
    service        VARCHAR(100),
    status         VARCHAR(30),
    tracking_code  VARCHAR(100)
);

CREATE TABLE metadata_pedido (
    order_uuid  VARCHAR(50) PRIMARY KEY REFERENCES pedido (uuid) ON DELETE CASCADE,
    source      VARCHAR(50),
    user_agent  TEXT,
    ip_address  VARCHAR(45)
);
